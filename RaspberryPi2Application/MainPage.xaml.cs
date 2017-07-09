using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RaspberryPi2Application
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly int SOUND_PIN = 4;
        private readonly int RELAY_PIN = 17;
        private readonly long shortTimeMin = 75;
        private readonly long shortTimeMax = 275;
        private readonly long longTimeMin = 150;
        private readonly long longTimeMax = 600;
        private GpioPin soundPin;
        private GpioPin relayPin;
        private GpioPinValue soundPinValue;
        private GpioPinValue relayPinValue;
        private bool soundReceived;
        private Stopwatch shortTimer;
        private Stopwatch longTimer;
        private int soundCount;
        private bool loop;
        private Task task;

        public MainPage()
        {
            InitializeComponent();
            shortTimer = new Stopwatch();
            longTimer = new Stopwatch();
            loop = true;
            InitGPIO();
            task = new Task(RunLoop);
            task.Start();
        }

        private void InitGPIO()
        {
            GpioController gpio = GpioController.GetDefault();
            relayPin = gpio.OpenPin(RELAY_PIN);
            relayPinValue = GpioPinValue.Low;
            relayPin.Write(relayPinValue);
            relayPin.SetDriveMode(GpioPinDriveMode.Output);
            soundPin = gpio.OpenPin(SOUND_PIN);
            soundPinValue = GpioPinValue.Low;
            soundPin.Write(soundPinValue);
            soundPin.SetDriveMode(GpioPinDriveMode.InputPullDown);
        }

        private void RunLoop()
        {
            while (loop)
            {
                soundPinValue = soundPin.Read();

                if (soundPinValue == GpioPinValue.Low)
                {
                    if (!shortTimer.IsRunning)
                        shortTimer.Start();

                    if (!longTimer.IsRunning)
                        longTimer.Start();

                    soundReceived = true;
                }

                else
                {

                    if (!soundReceived)
                        continue;

                    if (shortTimer.ElapsedMilliseconds <= shortTimeMax)
                    {
                        if (soundCount == 0)
                        {
                            soundCount++;
                            shortTimer.Stop();
                            shortTimer.Reset();
                        }

                        else if (soundCount == 1 && longTimer.ElapsedMilliseconds <= longTimeMax)
                        {
                            soundCount++;
                            SwitchRelay();
                        }

                        else
                        {
                            ResetValues();
                        }
                    }

                    else
                    {
                        ResetValues();
                    }

                    soundReceived = false;

                }
            }
        }

        private void SwitchRelay()
        {
            relayPinValue = relayPin.Read();
            relayPinValue = (relayPinValue == GpioPinValue.Low) ? GpioPinValue.High : GpioPinValue.Low;
            relayPin.Write(relayPinValue);
            ResetValues();
            task.Wait(2000);
        }

        private void ResetValues()
        {
            if (shortTimer.IsRunning)
            {
                shortTimer.Stop();
                shortTimer.Reset();
            }

            if (longTimer.IsRunning)
            {
                longTimer.Stop();
                longTimer.Reset();
            }

            if (soundCount > default(int))
                soundCount = default(int);
        }
    }
}
