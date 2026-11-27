using System;
using System.Collections.Generic;
using System.Linq;

namespace PracticeProblems
{
    /// <summary>
    /// Daily Practice Problem - 2026-11-26
    /// Topic: Abstract Classes, Interfaces, Events, Indexers, and Switch Expressions
    ///
    /// Problem Statement:
    /// Design a Smart Home Automation &amp; Energy Monitoring System:
    /// 1. Define focused interfaces:
    ///    - 'ISwitchable': IsPoweredOn, TurnOn(), TurnOff().
    ///    - 'IEnergyMetered': RatedWatts, GetHourlyEnergyKwh(PowerMode mode).
    /// 2. Create an abstract base class 'SmartDevice' implementing ISwitchable and IEnergyMetered:
    ///    - Validated properties: DeviceId, Name, Room, RatedWatts, IsPoweredOn, Mode (PowerMode).
    ///    - An event 'AlertRaised' (EventHandler&lt;DeviceAlertEventArgs&gt;) triggered via a protected helper.
    ///    - Abstract method 'CalculateCurrentDrawWatts()' and virtual method 'GetStatusReport()'.
    /// 3. Create derived device types:
    ///    - 'SmartLight': Supports BrightnessPercent (0-100) and scales power draw proportionally.
    ///    - 'SmartThermostat': Supports TargetTempCelsius (10-32°C) and draws peak watts when heating/cooling delta is large.
    ///    - 'SmartSecurityCamera' (sealed): Supports IsRecording, MotionSensitivity, and raises an alert when motion is detected.
    /// 4. Create a 'SmartHomeHub' class with:
    ///    - An indexer 'this[string deviceId]' for safe lookup.
    ///    - Methods to subscribe to device alerts, apply a global Eco mode, and compute total daily kWh using switch expressions.
    /// </summary>

    #region Enums & EventArgs

    /// <summary>
    /// Operating power profile for smart devices.
    /// </summary>
    public enum PowerMode
    {
        Eco,
        Normal,
        Performance
    }

    /// <summary>
    /// Event payload emitted when a smart device raises an operational or security alert.
    /// </summary>
    public sealed class DeviceAlertEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public string DeviceName { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }

        public DeviceAlertEventArgs(string deviceId, string deviceName, string message)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            Message = string.IsNullOrWhiteSpace(message) ? "Unspecified alert." : message.Trim();
            Timestamp = DateTime.Now;
        }
    }

    #endregion

    #region Interfaces

    /// <summary>
    /// Contract for devices that can be switched on and off remotely.
    /// </summary>
    public interface ISwitchable
    {
        bool IsPoweredOn { get; }
        void TurnOn();
        void TurnOff();
    }

    /// <summary>
    /// Contract for devices whose electrical consumption can be metered.
    /// </summary>
    public interface IEnergyMetered
    {
        double RatedWatts { get; }
        double GetHourlyEnergyKwh(PowerMode mode);
    }

    #endregion

    #region Base SmartDevice Class

    /// <summary>
    /// Abstract base class representing a networked smart home device.
    /// Encapsulates identity, power state, operating mode, and alert notifications.
    /// </summary>
    public abstract class SmartDevice : ISwitchable, IEnergyMetered
    {
        private string deviceId;
        private string name;
        private string room;
        private double ratedWatts;

        public string DeviceId
        {
            get => deviceId;
            protected set => deviceId = string.IsNullOrWhiteSpace(value) ? "DEV-000" : value.Trim();
        }

        public string Name
        {
            get => name;
            set => name = string.IsNullOrWhiteSpace(value) ? "Unnamed Device" : value.Trim();
        }

        public string Room
        {
            get => room;
            set => room = string.IsNullOrWhiteSpace(value) ? "Unassigned" : value.Trim();
        }

        public double RatedWatts
        {
            get => ratedWatts;
            protected set => ratedWatts = value > 0 ? value : 1.0;
        }

        public bool IsPoweredOn { get; protected set; }
        public PowerMode Mode { get; private set; }

        /// <summary>
        /// Raised whenever the device emits a diagnostic or security notification.
        /// </summary>
        public event EventHandler<DeviceAlertEventArgs> AlertRaised;

        protected SmartDevice(string deviceId, string name, string room, double ratedWatts, PowerMode mode = PowerMode.Normal)
        {
            DeviceId = deviceId;
            Name = name;
            Room = room;
            RatedWatts = ratedWatts;
            Mode = mode;
            IsPoweredOn = false;
        }

        public virtual void TurnOn()
        {
            IsPoweredOn = true;
        }

        public virtual void TurnOff()
        {
            IsPoweredOn = false;
        }

        public virtual void SetPowerMode(PowerMode newMode)
        {
            Mode = newMode;
        }

        /// <summary>
        /// Computes instantaneous wattage draw based on current device state.
        /// </summary>
        public abstract double CalculateCurrentDrawWatts();

        /// <summary>
        /// Estimates energy consumed over one hour in kilowatt-hours (kWh) for a given power mode.
        /// </summary>
        public virtual double GetHourlyEnergyKwh(PowerMode mode)
        {
            if (!IsPoweredOn)
            {
                return 0.0;
            }

            double modeFactor = mode switch
            {
                PowerMode.Eco => 0.70,
                PowerMode.Normal => 1.00,
                PowerMode.Performance => 1.25,
                _ => 1.00
            };

            return (CalculateCurrentDrawWatts() * modeFactor) / 1000.0;
        }

        /// <summary>
        /// Returns a formatted diagnostic status line for this device.
        /// </summary>
        public virtual string GetStatusReport()
        {
            string powerState = IsPoweredOn ? "ON" : "OFF";
            double kwh = GetHourlyEnergyKwh(Mode);
            return $"[{DeviceId}] {Name} ({Room}) | State: {powerState} | Mode: {Mode} | Draw: {CalculateCurrentDrawWatts():F1} W ({kwh:F4} kWh/hr)";
        }

        protected void OnAlertRaised(string message)
        {
            AlertRaised?.Invoke(this, new DeviceAlertEventArgs(DeviceId, Name, message));
        }
    }

    #endregion

    #region Derived Smart Devices

    /// <summary>
    /// Dimmable smart LED light fixture.
    /// </summary>
    public class SmartLight : SmartDevice
    {
        private int brightnessPercent;

        public int BrightnessPercent
        {
            get => brightnessPercent;
            set => brightnessPercent = Math.Clamp(value, 0, 100);
        }

        public SmartLight(string deviceId, string name, string room, double ratedWatts, int brightnessPercent = 80)
            : base(deviceId, name, room, ratedWatts)
        {
            BrightnessPercent = brightnessPercent;
        }

        public override void SetPowerMode(PowerMode newMode)
        {
            base.SetPowerMode(newMode);
            if (newMode == PowerMode.Eco && BrightnessPercent > 50)
            {
                BrightnessPercent = 50;
            }
        }

        public override double CalculateCurrentDrawWatts()
        {
            if (!IsPoweredOn || BrightnessPercent == 0)
            {
                return 0.0;
            }

            // Standby controller overhead of 0.5 W plus proportional LED load.
            return 0.5 + (RatedWatts * (BrightnessPercent / 100.0));
        }

        public override string GetStatusReport()
        {
            return $"{base.GetStatusReport()} | Brightness: {BrightnessPercent}%";
        }
    }

    /// <summary>
    /// Climate control thermostat that scales HVAC power draw by temperature differential.
    /// </summary>
    public class SmartThermostat : SmartDevice
    {
        private double ambientTempCelsius;
        private double targetTempCelsius;

        public double AmbientTempCelsius
        {
            get => ambientTempCelsius;
            set => ambientTempCelsius = Math.Clamp(value, -10.0, 50.0);
        }

        public double TargetTempCelsius
        {
            get => targetTempCelsius;
            set
            {
                targetTempCelsius = Math.Clamp(value, 10.0, 32.0);
                if (Math.Abs(targetTempCelsius - AmbientTempCelsius) >= 6.0)
                {
                    OnAlertRaised($"Large temperature delta ({Math.Abs(targetTempCelsius - AmbientTempCelsius):F1}°C) may increase HVAC load.");
                }
            }
        }

        public SmartThermostat(
            string deviceId,
            string name,
            string room,
            double ratedWatts,
            double ambientTempCelsius,
            double targetTempCelsius)
            : base(deviceId, name, room, ratedWatts)
        {
            AmbientTempCelsius = ambientTempCelsius;
            this.targetTempCelsius = Math.Clamp(targetTempCelsius, 10.0, 32.0);
        }

        public override double CalculateCurrentDrawWatts()
        {
            if (!IsPoweredOn)
            {
                return 0.0;
            }

            double delta = Math.Abs(TargetTempCelsius - AmbientTempCelsius);
            if (delta < 0.5)
            {
                // Idle fan/sensor draw when target temperature is reached.
                return 5.0;
            }

            double loadRatio = Math.Min(1.0, delta / 8.0);
            return RatedWatts * loadRatio;
        }

        public override string GetStatusReport()
        {
            return $"{base.GetStatusReport()} | Ambient: {AmbientTempCelsius:F1}°C -> Target: {TargetTempCelsius:F1}°C";
        }
    }

    /// <summary>
    /// Sealed security camera with recording state and motion alert events.
    /// </summary>
    public sealed class SmartSecurityCamera : SmartDevice
    {
        public bool IsRecording { get; private set; }
        public int MotionSensitivity { get; set; }

        public SmartSecurityCamera(string deviceId, string name, string room, double ratedWatts, int motionSensitivity = 7)
            : base(deviceId, name, room, ratedWatts, PowerMode.Performance)
        {
            MotionSensitivity = Math.Clamp(motionSensitivity, 1, 10);
        }

        public override void TurnOn()
        {
            base.TurnOn();
            IsRecording = true;
        }

        public override void TurnOff()
        {
            IsRecording = false;
            base.TurnOff();
            OnAlertRaised("Camera powered off; continuous recording stopped.");
        }

        /// <summary>
        /// Simulates a motion detection trigger and raises an alert when active.
        /// </summary>
        public void DetectMotion(string zone)
        {
            if (!IsPoweredOn)
            {
                return;
            }

            string cleanZone = string.IsNullOrWhiteSpace(zone) ? "Unknown Zone" : zone.Trim();
            OnAlertRaised($"Motion detected in '{cleanZone}' (Sensitivity {MotionSensitivity}/10).");
        }

        public override double CalculateCurrentDrawWatts()
        {
            if (!IsPoweredOn)
            {
                return 0.0;
            }

            return IsRecording ? RatedWatts : RatedWatts * 0.4;
        }

        public override string GetStatusReport()
        {
            string recState = IsRecording ? "Recording" : "Standby";
            return $"{base.GetStatusReport()} | Camera: {recState}";
        }
    }

    #endregion

    #region SmartHomeHub Coordinator

    /// <summary>
    /// Central hub managing smart devices, alert subscriptions, and energy projections.
    /// Demonstrates indexers, collections, LINQ aggregation, and pattern matching.
    /// </summary>
    public class SmartHomeHub
    {
        private readonly List<SmartDevice> devices = new List<SmartDevice>();
        private readonly List<string> alertLog = new List<string>();

        public IReadOnlyList<SmartDevice> Devices => devices.AsReadOnly();
        public IReadOnlyList<string> AlertLog => alertLog.AsReadOnly();

        /// <summary>
        /// Indexer providing case-insensitive lookup by DeviceId.
        /// </summary>
        public SmartDevice this[string deviceId]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return null;
                }

                return devices.FirstOrDefault(d =>
                    d.DeviceId.Equals(deviceId.Trim(), StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Registers a device and subscribes the hub to its alerts.
        /// </summary>
        public void RegisterDevice(SmartDevice device)
        {
            if (device == null || this[device.DeviceId] != null)
            {
                return;
            }

            device.AlertRaised += HandleDeviceAlert;
            devices.Add(device);
        }

        public void PowerOnAll()
        {
            foreach (SmartDevice device in devices)
            {
                device.TurnOn();
            }
        }

        /// <summary>
        /// Switches lighting and climate devices to Eco mode while keeping security cameras active.
        /// </summary>
        public void ApplyNightEcoPolicy()
        {
            foreach (SmartDevice device in devices)
            {
                switch (device)
                {
                    case SmartLight light:
                        light.SetPowerMode(PowerMode.Eco);
                        break;
                    case SmartThermostat thermostat:
                        thermostat.SetPowerMode(PowerMode.Eco);
                        break;
                    case SmartSecurityCamera camera:
                        camera.SetPowerMode(PowerMode.Performance);
                        break;
                }
            }
        }

        /// <summary>
        /// Calculates projected 24-hour energy usage across all registered devices.
        /// </summary>
        public double CalculateProjectedDailyKwh()
        {
            return devices.Sum(d => d.GetHourlyEnergyKwh(d.Mode) * 24.0);
        }

        private void HandleDeviceAlert(object sender, DeviceAlertEventArgs e)
        {
            alertLog.Add($"[{e.Timestamp:HH:mm:ss}] {e.DeviceId} ({e.DeviceName}): {e.Message}");
        }
    }

    #endregion

    #region Problem Solution & Verification Runner

    /// <summary>
    /// Driver class verifying smart device polymorphism, event alerts, indexer access, and energy calculations.
    /// </summary>
    public static class Day20261126Solution
    {
        /// <summary>
        /// Runs the end-to-end Smart Home Automation &amp; Energy Monitoring scenario.
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("===========================================================");
            Console.WriteLine(" Daily Practice Problem: 2026-11-26 Solution");
            Console.WriteLine("===========================================================\n");

            var hub = new SmartHomeHub();
            var livingRoomLight = new SmartLight("LGT-101", "Living Room Ceiling", "Living Room", 18.0, 90);
            var mainThermostat = new SmartThermostat("THM-201", "Main HVAC", "Hallway", 1800.0, 27.5, 22.0);
            var frontCamera = new SmartSecurityCamera("CAM-301", "Front Porch Cam", "Exterior", 12.0, 8);

            hub.RegisterDevice(livingRoomLight);
            hub.RegisterDevice(mainThermostat);
            hub.RegisterDevice(frontCamera);

            hub.PowerOnAll();

            Console.WriteLine("Initial Device Status:");
            foreach (SmartDevice device in hub.Devices)
            {
                Console.WriteLine($"  {device.GetStatusReport()}");
            }
            Console.WriteLine($"Projected Daily Energy (Normal): {hub.CalculateProjectedDailyKwh():F3} kWh\n");

            // Trigger runtime events and policy updates
            if (hub["THM-201"] is SmartThermostat thermostat)
            {
                thermostat.TargetTempCelsius = 19.0; // Triggers high-delta alert
            }

            if (hub["CAM-301"] is SmartSecurityCamera camera)
            {
                camera.DetectMotion("Front Gate");
            }

            hub.ApplyNightEcoPolicy();

            Console.WriteLine("After Night Eco Policy:");
            foreach (SmartDevice device in hub.Devices)
            {
                Console.WriteLine($"  {device.GetStatusReport()}");
            }
            Console.WriteLine($"Projected Daily Energy (Night Eco): {hub.CalculateProjectedDailyKwh():F3} kWh\n");

            Console.WriteLine("Captured Hub Alerts:");
            foreach (string entry in hub.AlertLog)
            {
                Console.WriteLine($"  {entry}");
            }

            Console.WriteLine("\n===========================================================");
        }
    }

    #endregion
}
