using System.Net;
using System.Net.Sockets;

using ABB.Robotics.Math;
using ABB.Robotics.RobotStudio;
using ABB.Robotics.RobotStudio.Stations;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatrixComm
{
    /// <summary>
    /// Code-behind class for the MatrixComm Smart Component.
    /// </summary>
    /// <remarks>
    /// The code-behind class should be seen as a service provider used by the 
    /// Smart Component runtime. Only one instance of the code-behind class
    /// is created, regardless of how many instances there are of the associated
    /// Smart Component.
    /// Therefore, the code-behind class should not store any state information.
    /// Instead, use the SmartComponent.StateCache collection.
    /// </remarks>
    public class CodeBehind : SmartComponentCodeBehind
    {
        #region Properties

        private IPEndPoint endPoint { get; set; }

        private Socket commSocket { get; set; }

        const int msgSize = 64;
        private static byte[] msgBuffer = new byte[msgSize];

        private SmartComponent _component { get; set; }

        #endregion

        #region Initialization

        public CodeBehind()
        {
            // code behind initialization
        }

        private void InitializeSocket(String IP, int port)
        {
            endPoint = new IPEndPoint(address: IPAddress.Parse(IP), port: port);

            commSocket = new Socket(endPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            commSocket.Connect(endPoint);

        }

        private void CloseSocket()
        {
            //commSocket.Shutdown(SocketShutdown.Both);
            commSocket.Close();
        }

        #endregion


        /// <summary>
        /// Called when the value of a dynamic property value has changed.
        /// </summary>
        /// <param name="component"> Component that owns the changed property. </param>
        /// <param name="changedProperty"> Changed property. </param>
        /// <param name="oldValue"> Previous value of the changed property. </param>
        public override void OnPropertyValueChanged(SmartComponent component, DynamicProperty changedProperty, Object oldValue)
        {
        }

        public override void OnLoad(SmartComponent component)
        {
            Logger.AddMessage(new LogMessage($"{component.Name} component loaded! Simulation must be running in order for it to work!"));
        }

        /// <summary>
        /// Called when the value of an I/O signal value has changed.
        /// </summary>
        /// <param name="component"> Component that owns the changed signal. </param>
        /// <param name="changedSignal"> Changed signal. </param>
        public override void OnIOSignalValueChanged(SmartComponent component, IOSignal changedSignal)
        {
            if (changedSignal.Name.Equals("Activate"))
            {
                if (changedSignal.Value.Equals(1))
                {
                    try
                    {
                        InitializeSocket((String)component.Properties["IP"].Value, (int)component.Properties["port"].Value);
                    }
                    catch (SocketException)
                    {
                        component.IOSignals["Activate"].Value = false;
                    }
                    catch (Exception)
                    {
                        throw;
                    }    
                }
                else
                {
                    CloseSocket();
                }
            }
        }

        /// <summary>
        /// Called during simulation.
        /// </summary>
        /// <param name="component"> Simulated component. </param>
        /// <param name="simulationTime"> Time (in ms) for the current simulation step. </param>
        /// <param name="previousTime"> Time (in ms) for the previous simulation step. </param>
        /// <remarks>
        /// For this method to be called, the component must be marked with
        /// simulate="true" in the xml file.
        /// </remarks>
        public override void OnSimulationStep(SmartComponent component, double simulationTime, double previousTime)
        {
            // if activated
            if (component.IOSignals["Activate"].Value.Equals(1))
            {
                // if there is data to be read
                if (commSocket.Available > 0)
                {
                    commSocket.Receive(msgBuffer, msgSize, SocketFlags.None);

                    var matrix = "";
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            matrix += String.Format("{0:0.00}",BitConverter.ToSingle(msgBuffer, 4*i + j)).PadRight(16);
                        }
                        matrix += "\r\n";

                    }
                    component.Properties["Matrix"].Value = matrix;

                    commSocket.Send(msgBuffer);
                }
            }
        }
    }
}
