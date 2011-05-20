using System;
using System.Collections.Generic;
using Encog.Engine.Network.Activation;
using Encog.ML;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;

namespace Encog.Neural.Pattern
{
    /// <summary>
    /// Used to create feedforward neural networks. A feedforward network has an
    /// input and output layers separated by zero or more hidden layers. The
    /// feedforward neural network is one of the most common neural network patterns.
    /// </summary>
    ///
    public class FeedForwardPattern : NeuralNetworkPattern
    {
        /// <summary>
        /// The number of hidden neurons.
        /// </summary>
        ///
        private readonly IList<Int32> hidden;

        /// <summary>
        /// The activation function.
        /// </summary>
        ///
        private IActivationFunction activationHidden;

        /// <summary>
        /// The activation function.
        /// </summary>
        ///
        private IActivationFunction activationOutput;

        /// <summary>
        /// The number of input neurons.
        /// </summary>
        ///
        private int inputNeurons;

        /// <summary>
        /// The number of output neurons.
        /// </summary>
        ///
        private int outputNeurons;

        /// <summary>
        /// Construct the object.
        /// </summary>
        public FeedForwardPattern()
        {
            hidden = new List<Int32>();
        }

        /// <value>the activationOutput to set</value>
        public IActivationFunction ActivationOutput
        {
            get { return activationOutput; }
            set { activationOutput = value; }
        }

        #region NeuralNetworkPattern Members

        /// <summary>
        /// Add a hidden layer, with the specified number of neurons.
        /// </summary>
        public void AddHiddenLayer(int count)
        {
            hidden.Add(count);
        }

        /// <summary>
        /// Clear out any hidden neurons.
        /// </summary>
        ///
        public void Clear()
        {
            hidden.Clear();
        }

        /// <summary>
        /// Generate the feedforward neural network.
        /// </summary>
        public MLMethod Generate()
        {
            if (activationOutput == null)
                activationOutput = activationHidden;

            Layer input = new BasicLayer(null, true, inputNeurons);

            var result = new BasicNetwork();
            result.AddLayer(input);


            foreach (Int32 count  in  hidden)
            {
                Layer hidden_0 = new BasicLayer(activationHidden, true,
                                                (count));

                result.AddLayer(hidden_0);
            }

            Layer output = new BasicLayer(activationOutput, false,
                                          outputNeurons);
            result.AddLayer(output);

            result.Structure.FinalizeStructure();
            result.Reset();

            return result;
        }

        /// <summary>
        /// Set the activation function to use on each of the layers.
        /// </summary>
        public IActivationFunction ActivationFunction
        {
            set { activationHidden = value; }
        }


        /// <summary>
        /// Set the number of input neurons.
        /// </summary>
        public int InputNeurons
        {
            set { inputNeurons = value; }
        }


        /// <summary>
        /// Set the number of output neurons.
        /// </summary>
        public int OutputNeurons
        {
            set { outputNeurons = value; }
        }

        #endregion
    }
}