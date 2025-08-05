using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;

public class App
{
    public static void Main(string[] args)
    {
        Temperature lol = new Temperature();
        IReadOnlyDictionary<string, float> temps = lol.GetTemperaturesInCelsius();

        foreach (KeyValuePair<string, float> temp in temps)
        {
            Console.WriteLine($"{temp.Key} - {temp.Value}");
        }

        Console.ReadLine();
    }
}

public class Temperature
{
    private readonly Computer _computer;

    public Temperature()
    {
        _computer = new Computer { CPUEnabled = true };
        _computer.Open();
    }

    public IReadOnlyDictionary<string, float> GetTemperaturesInCelsius()
    {
        var coreAndTemperature = new Dictionary<string, float>();

        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update(); //use hardware.Name to get CPU model
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                    coreAndTemperature.Add(sensor.Name, sensor.Value.Value);
            }
        }

        return coreAndTemperature;
    }

    public void Dispose()
    {
        try
        {
            _computer.Close();
        }
        catch (Exception)
        {
            //ignore closing errors
        }
    }
}