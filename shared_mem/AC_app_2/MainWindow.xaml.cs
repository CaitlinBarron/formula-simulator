using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using AssettoCorsaSharedMemory;
using System.Globalization;

namespace AC_app_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AssettoCorsa acSession;         //this is the connection to the actual instance of the game
        SerialPort _port;               //this is the connection to the serial port
        ulong pitchRollCount = 0;
        string pitchPosCommand = "\u0002P:+{0}{1}{2}\u0003";    //STX P : + <3 digits> ETX
        string pitchNegCommand = "\u0002P:-{0}{1}{2}\u0003";    //STX P : - <3 digits> ETX
        string rollPosCommand = "\u0002R:+{0}{1}{2}\u0003";     //STX R : + <3 digits> ETX
        string rollNegCommand = "\u0002R:-{0}{1}{2}\u0003";     //STX R : - <3 digits> ETX
        string zeroCommand = "\u0002Z\u0003";                     //STX Z ETX
        string stopCommand = "\u0002S\u0003";                     //STX S ETX
        string ack = "\u0002\u0006\u0003";
        float pitch;
        float roll;
        AC_STATUS gameStat;             //this should have the game status (paused, running, etc)
        bool controllerReady = true;

        public MainWindow()
        {
            //set up initial values and states for the GUI parts
            InitializeComponent();
            this.Closed += MainWindow_Closed;
            string[] portNames = SerialPort.GetPortNames();
            portCombo.ItemsSource = portNames;
            statusBar.Text = "Motion settings can not be changed during a race";
            portCombo.IsEditable = false;
            motionSlider.IsEnabled = false;
            disconnectBtn.IsEnabled = false;
            scaleBox.IsEnabled = false;
            controllerReady = true;


            pitch = 0;
            roll = 0;
            gameStat = AC_STATUS.AC_OFF;

            acSession = new AssettoCorsa();
            // these three values determine how frequently the values in the game shared memory are checked
            acSession.PhysicsInterval = 10;        // these are in milliseconds
            acSession.GraphicsInterval = 10000;
            acSession.StaticInfoInterval = 5000;

            acSession.PhysicsUpdated += AC_PhysicsUpdated; // Add event listener for StaticInfo
            acSession.GameStatusChanged += AC_GameStatusChanged;

            acSession.Start(); // Connect to shared memory and start interval timers 
            Console.Read();
            
        }


        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Show all the incoming data in the port's buffer
            String dataIn = "";
            dataIn = _port.ReadExisting();
            Console.WriteLine(dataIn);
            if (dataIn.Contains("\u0006"))
            {
                controllerReady = true;
                Console.WriteLine("ack recieved");
            }
        }


        //This is the main function where the gameplay stuff happens vvv
        //this function is called periodically based on the acSession.PhysicsInterval variable
        private void AC_PhysicsUpdated(object sender, PhysicsEventArgs e)
        {
            string pitchForm = pitchNegCommand;
            string rollForm = rollNegCommand;
            float pitch = e.Physics.Pitch * (180 / Convert.ToSingle(Math.PI));
            float roll = e.Physics.Roll * (180 / Convert.ToSingle(Math.PI));

            //check status of game via graphics
            Graphics graphicsData = acSession.ReadGraphics();
            AC_STATUS currStat = graphicsData.Status;
            if (currStat != gameStat)
            {
                //update game status if changed
                gameStat = currStat;
            }

            //determine which command to send to the system
            if (pitch >= 0)
            {
                pitchForm = pitchPosCommand;
            }

            if (roll >= 0)
            {
                rollForm = rollPosCommand;
            }
            
            //format the new game angle to match the expected serial formatting
            string pitchStr = pitch.ToString("00.0");
            string rollStr = roll.ToString("00.0");
            string pitchStrTrim = pitchStr.Replace("-", "");
            string rollStrTrim = rollStr.Replace("-", "");
            string fullPitch = String.Format(pitchForm, pitchStrTrim[0], pitchStrTrim[1], pitchStrTrim[3]);
            string fullRoll = String.Format(rollForm, rollStrTrim[0], rollStrTrim[1], rollStrTrim[3]);

            //print lines for debugging
            Console.WriteLine("Pitch = " + pitchStr + "°, Roll = " + rollStr + "° \n");
            Console.WriteLine("accel = " + e.Physics.SpeedKmh); //this is ~0 when in menu and pits but paused is constant
            try
            {
                if (_port.IsOpen && controllerReady)
                {
                    controllerReady = false;
                    pitchRollCount++;

                    //send either the pitch value or roll value
                    if (pitchRollCount%2 == 1)
                    {
                        Console.WriteLine("sending pitch commands");
                        _port.Write(fullPitch);
                    }
                    else
                    {
                        Console.WriteLine("sending roll commands");
                        _port.Write(fullRoll);
                    }
                }   
            }
            catch (Exception ex)
            {
                Console.WriteLine("error writing to port: " + ex.Message);
            }
        }


        private void AC_GameStatusChanged(object sender, EventArgs e)
        {
            Console.WriteLine("status changed = " + e.ToString() + "\n");
        }


        private void connectBtn_Click(object sender, RoutedEventArgs e)
        {
            //toggle buttons
            connectBtn.IsEnabled = false;
            disconnectBtn.IsEnabled = true;
            portCombo.IsEnabled = false;
            try
            {
                //open connection to serial port
                _port = new SerialPort(portCombo.Text, 256000);
                _port.Open();
                _port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "cannot connect to " + portCombo.Text, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }


        private void disconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //close serial connection
                _port.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                //toggle buttons
                connectBtn.IsEnabled = true;
                disconnectBtn.IsEnabled = false;
                portCombo.IsEnabled = true;
            }
        }


        private void scaleCheck_Changed(object sender, RoutedEventArgs e)
        {
            //the scale function was intended to be used to scale the motion down incase the rider had motino sickness or was sensitive
            if (scaleCheck.IsChecked == true)
            {
                motionSlider.IsEnabled = true;
                scaleBox.IsEnabled = true;
            }
            else
            {
                motionSlider.IsEnabled = false;
                scaleBox.IsEnabled = false;
            }
        }


        private void motionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if slider moved, update text box to show new value
            scaleBox.Text = Math.Round((decimal)motionSlider.Value, 0).ToString();
        }


        private void scaleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if txt value changed, move slide to match new value
            int currNum = (int) motionSlider.Value;
            int val = 0;
            bool isNum = int.TryParse(scaleBox.Text, out val);
            if(isNum == true)
            {
                if(val >= 100)
                {
                    scaleBox.Text = "100";
                    motionSlider.Value = 100;
                }
                else if(val <= 0)
                {
                    scaleBox.Text = "0";
                    motionSlider.Value = 0;
                }
                else
                {
                    motionSlider.Value = val;
                }
            }
            else
            {
                scaleBox.Text = currNum.ToString();
            }
        }


        private void MainWindow_Closed(object sender, EventArgs e)
        {
            //in case the app is closed before the port is diconeccted
            if (_port != null && _port.IsOpen)
            {
                _port.Close();
            }
        }

        private void forwardBtn_Click(object sender, RoutedEventArgs e)
        {
            string pitch = "01.0"; // -01.0 deg
            string roll = "00.0"; // 0 deg
            string fullPitch = String.Format(pitchNegCommand, pitch[0], pitch[1], pitch[3]);
            string fullRoll = String.Format(rollPosCommand, roll[0], roll[1], roll[3]);
            try
            {
                if (_port.IsOpen)
                {
                    Console.WriteLine("sending full forward command to port");
                    //_port.Write(fullPitch);
                    //_port.Write(fullRoll);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error writing to port: " + ex.Message);
            }
        }

        private void rightBtn_Click(object sender, RoutedEventArgs e)
        {
            string pitch = "00.0"; // 0 deg
            string roll = "01.0"; // +01.0 deg
            string fullPitch = String.Format(pitchPosCommand, pitch[0], pitch[1], pitch[3]);
            string fullRoll = String.Format(rollPosCommand, roll[0], roll[1], roll[3]);
            try
            {
                if (_port.IsOpen)
                {
                    Console.WriteLine("sending full forward command to port");
                    //_port.Write(fullPitch);
                    _port.Write(fullRoll);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error writing to port: " + ex.Message);
            }
        }

        private void leftBtn_Click(object sender, RoutedEventArgs e)
        {
            string pitch = "00.0"; // 0 deg
            string roll = "01.0"; // -01.0 deg
            string fullPitch = String.Format(pitchPosCommand, pitch[0], pitch[1], pitch[3]);
            string fullRoll = String.Format(rollNegCommand, roll[0], roll[1], roll[3]);
            try
            {
                if (_port.IsOpen)
                {
                    Console.WriteLine("sending full forward command to port");
                    //_port.Write(fullPitch);
                    _port.Write(fullRoll);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error writing to port: " + ex.Message);
            }
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            string pitch = "01.0"; // +01.0 deg
            string roll = "00.0"; // 0 deg
            string fullPitch = String.Format(pitchPosCommand, pitch[0], pitch[1], pitch[3]);
            string fullRoll = String.Format(rollPosCommand, roll[0], roll[1], roll[3]);
            try
            {
                if (_port.IsOpen)
                {
                    Console.WriteLine("sending full forward command to port");
                    //_port.Write(fullPitch);
                    //_port.Write(fullRoll);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error writing to port: " + ex.Message);
            }
        }
    }
}
