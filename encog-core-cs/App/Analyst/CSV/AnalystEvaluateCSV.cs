using System;
using System.IO;
using System.Text;
using Encog.App.Analyst.CSV.Basic;
using Encog.App.Analyst.CSV.Normalize;
using Encog.App.Analyst.Script.Normalize;
using Encog.App.Analyst.Util;
using Encog.App.Quant;
using Encog.ML;
using Encog.ML.Data;
using Encog.ML.Data.Basic;
using Encog.Util.Arrayutil;
using Encog.Util.CSV;

namespace Encog.App.Analyst.CSV
{
    /// <summary>
    /// Used by the analyst to evaluate a CSV file.
    /// </summary>
    ///
    public class AnalystEvaluateCSV : BasicFile
    {
        /// <summary>
        /// The analyst to use.
        /// </summary>
        ///
        private EncogAnalyst _analyst;

        /// <summary>
        /// The headers.
        /// </summary>
        ///
        private CSVHeaders _analystHeaders;

        /// <summary>
        /// The number of columns in the file.
        /// </summary>
        ///
        private int _fileColumns;

        /// <summary>
        /// The number of output columns.
        /// </summary>
        ///
        private int _outputColumns;

        /// <summary>
        /// Used to handle time series.
        /// </summary>
        ///
        private TimeSeriesUtil _series;

        /// <summary>
        /// Analyze the data. This counts the records and prepares the data to be
        /// processed.
        /// </summary>
        ///
        /// <param name="theAnalyst">The analyst to use.</param>
        /// <param name="inputFile">The input file.</param>
        /// <param name="headers">True if headers are present.</param>
        /// <param name="format">The format.</param>
        public void Analyze(EncogAnalyst theAnalyst,
                            FileInfo inputFile, bool headers, CSVFormat format)
        {
            InputFilename = inputFile;
            ExpectInputHeaders = headers;
            InputFormat = format;

            Analyzed = true;
            _analyst = theAnalyst;

            PerformBasicCounts();
            _fileColumns = InputHeadings.Length;
            _outputColumns = _analyst.DetermineOutputFieldCount();

            _analystHeaders = new CSVHeaders(InputHeadings);
            _series = new TimeSeriesUtil(_analyst,
                                        _analystHeaders.Headers);
        }

        /// <summary>
        /// Prepare the output file, write headers if needed.
        /// </summary>
        ///
        /// <param name="outputFile">The output file.</param>
        ///<returns>The file to write to.</returns>
        private new StreamWriter PrepareOutputFile(FileInfo outputFile)
        {
            try
            {
                outputFile.Delete();
                var tw = new StreamWriter(outputFile.OpenWrite());

                // write headers, if needed
                if (ProduceOutputHeaders)
                {
                    var line = new StringBuilder();


                    // handle provided fields, not all may be used, but all should
                    // be displayed
                    foreach (String heading  in  InputHeadings)
                    {
                        AppendSeparator(line, OutputFormat);
                        line.Append("\"");
                        line.Append(heading);
                        line.Append("\"");
                    }


                    // now add the output fields that will be generated
                    foreach (AnalystField field  in  _analyst.Script.Normalize.NormalizedFields)
                    {
                        if (field.Output && !field.Ignored)
                        {
                            AppendSeparator(line, OutputFormat);
                            line.Append("\"Output:");
                            line.Append(CSVHeaders.TagColumn(field.Name, 0,
                                                             field.TimeSlice, false));
                            line.Append("\"");
                        }
                    }

                    tw.WriteLine(line.ToString());
                }

                return tw;
            }
            catch (IOException e)
            {
                throw new QuantError(e);
            }
        }

        /// <summary>
        /// Process the file.
        /// </summary>
        ///
        /// <param name="outputFile">The output file.</param>
        /// <param name="method">THe method to use.</param>
        public void Process(FileInfo outputFile, MLMethod method)
        {
            var csv = new ReadCSV(InputFilename.ToString(),
                                  ExpectInputHeaders, InputFormat);

            MLData output;

            int outputLength = _analyst.DetermineUniqueColumns();

            StreamWriter tw = PrepareOutputFile(outputFile);

            ResetStatus();
            while (csv.Next())
            {
                UpdateStatus(false);
                var row = new LoadedRow(csv, _outputColumns);

                double[] inputArray = AnalystNormalizeCSV.ExtractFields(_analyst,
                                                                        _analystHeaders, csv, outputLength, false);
                if (_series.TotalDepth > 1)
                {
                    inputArray = _series.Process(inputArray);
                }

                if (inputArray != null)
                {
                    MLData input = new BasicMLData(inputArray);

                    // evaluation data
                    if ((method is MLClassification)
                        && !(method is MLRegression))
                    {
                        // classification only?
                        output = new BasicMLData(1);
                        output[0] =
                            ((MLClassification) method).Classify(input);
                    }
                    else
                    {
                        // regression
                        output = ((MLRegression) method).Compute(input);
                    }

                    // skip file data
                    int index = _fileColumns;
                    int outputIndex = 0;


                    // display output
                    foreach (AnalystField field  in  _analyst.Script.Normalize.NormalizedFields)
                    {
                        if (_analystHeaders.Find(field.Name) != -1)
                        {
                            if (field.Output)
                            {
                                if (field.Classify)
                                {
                                    // classification
                                    ClassItem cls = field.DetermineClass(
                                        outputIndex, output.Data);
                                    outputIndex += field.ColumnsNeeded;
                                    if (cls == null)
                                    {
                                        row.Data[index++] = "?Unknown?";
                                    }
                                    else
                                    {
                                        row.Data[index++] = cls.Name;
                                    }
                                }
                                else
                                {
                                    // regression
                                    double n = output[outputIndex++];
                                    n = field.DeNormalize(n);
                                    row.Data[index++] = InputFormat
                                        .Format(n, Precision);
                                }
                            }
                        }
                    }
                }

                WriteRow(tw, row);
            }
            ReportDone(false);
            tw.Close();
            csv.Close();
        }
    }
}