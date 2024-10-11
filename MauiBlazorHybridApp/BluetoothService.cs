using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
//using Windows.Management.Deployment;

public class BluetoothService
{
    private readonly IAdapter _adapter;
    private readonly IBluetoothLE _bluetoothLE;

    public BluetoothService()
    {
        _bluetoothLE = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
    }

    public async Task ScanBLEDevicesAsync(List<string> Devices)
    {
        try
        {
            // Check and request Bluetooth permissions
            var status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Bluetooth>();
            }
            if (status != PermissionStatus.Granted)
            {
                Debug.WriteLine("Bluetooth permission not granted.");
                Devices.Add("Bluetooth permission not granted.");
                return;
            }
            status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            if (status != PermissionStatus.Granted)
            {
                Debug.WriteLine("Location permission not granted.");
                Devices.Add("Location permission not granted.");
                return;
            }
            if (!_bluetoothLE.IsOn)
            {
                Debug.WriteLine("Bluetooth is off. Please turn it on.");
                Devices.Add("Bluetooth is off. Please turn it on.");
                return;
            }
            _adapter.DeviceDiscovered += (s, a) =>
            {
                Debug.WriteLine($"Device found: {a.Device.Name}");
                Devices.Add(a.Device.Name);
            };
            await _adapter.StartScanningForDevicesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error scanning for devices: {ex.Message}");
            Devices.Add("Error scanning for devices");
            Devices.Add(ex.Message);
        }
    }

    public async Task<bool> ConnectToDeviceAsync(string deviceName)
    {
        try
        {
            // Check and request Bluetooth permissions
            var status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (status!= PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Bluetooth>();
            }
            if (status != PermissionStatus.Granted)
            {
                Console.WriteLine("Bluetooth permission not granted.");
            }
            status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if(status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            if (status != PermissionStatus.Granted)
            {
                Console.WriteLine("Location permission not granted.");
                return false;
            }

            if (!_bluetoothLE.IsOn)
            {
                Console.WriteLine("Bluetooth is off. Please turn it on.");
                return false;
            }

            var devices = _adapter.GetSystemConnectedOrPairedDevices();
            var device = devices.FirstOrDefault(d => d.Name == deviceName);

            if (device == null)
            {
                Debug.WriteLine($"Device {deviceName} not found. Scanning for devices...");
                _adapter.DeviceDiscovered += (s, a) =>
                {
                    if (a.Device.Name == deviceName)
                    {
                        device = a.Device;
                        _adapter.StopScanningForDevicesAsync();
                    }
                    else
                    { 
                        Debug.WriteLine($"Not Device: {a.Device.Name}"); 
                    }
                };

                await _adapter.StartScanningForDevicesAsync();
            }

            if (device != null)
            {
                Debug.WriteLine($"Connecting to {deviceName}...");    
                await _adapter.ConnectToDeviceAsync(device);
                Debug.WriteLine($"Connected to {deviceName}");
                return true;
            }
            else
            {
                Console.WriteLine($"Device {deviceName} not found.");
                return false;
            }
        }
        catch (DeviceConnectionException ex)
        {
            Console.WriteLine($"Error connecting to device: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectDeviceAsync()
    {
        try
        {
            await _adapter.DisconnectDeviceAsync(_adapter.ConnectedDevices.FirstOrDefault());
        }
        catch (DeviceConnectionException ex)
        {
            Console.WriteLine($"Error disconnecting from device: {ex.Message}");
        }
    }

    public async Task GetServicesAsync(List<string> serviceIds)
    {
        try
        {
            var device = _adapter.ConnectedDevices.FirstOrDefault();
            if (device == null)
            {
                Debug.WriteLine("No connected device found.");
                return;
            }
            var services = await device.GetServicesAsync();
            foreach (var service in services)
            {
                Debug.WriteLine($"Service: {service.Id}");
                serviceIds.Add(service.Id.ToString());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting services: {ex.Message}");
        }
    }

    public async Task GetCharacteristicsAsync(string serviceId, List<string> characteristicIds)
    {
        try
        {
            var device = _adapter.ConnectedDevices.FirstOrDefault();
            if (device == null)
            {
                Debug.WriteLine("No connected device found.");
                return;
            }
            var service = await device.GetServiceAsync(Guid.Parse(serviceId));
            var characteristics = await service.GetCharacteristicsAsync();
            foreach (var characteristic in characteristics)
            {
                Debug.WriteLine($"Characteristic: {characteristic.Id}");
                characteristicIds.Add(characteristic.Id.ToString());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting characteristics: {ex.Message}");
        }
    }

    public async Task<string> ReadCharacteristicAsync(string serviceId, string characteristicId)
    {
        try
        {
            var device = _adapter.ConnectedDevices.FirstOrDefault();
            if (device == null)
            {
                Console.WriteLine("No connected device found.");
                return "device not found";
            }
            var service = await device.GetServiceAsync(Guid.Parse(serviceId));
            var characteristic = await service.GetCharacteristicAsync(Guid.Parse(characteristicId));
            var bytes = await characteristic.ReadAsync();
            if (bytes.data == null)
            {
                return "No data read from characteristic.";
            }
            else
            {
                Console.WriteLine($"Read {bytes.data.Length} bytes from characteristic.");
                var value = BitConverter.ToString(bytes.data);
                Debug.WriteLine($"Read value: {value}");
                return value;
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading characteristic: {ex.Message}");
            return $"Error reading characteristic: {ex.Message}";
        }
    }

   public async Task WriteCharacteristicAsync()
    {
        try
        {
            var device = _adapter.ConnectedDevices.FirstOrDefault();
            if (device == null)
            {
                Console.WriteLine("No connected device found.");
                return;
            }
            var service = await device.GetServiceAsync(Guid.Parse("0000180f-0000-1000-8000-00805f9b34fb"));
            var characteristic = await service.GetCharacteristicAsync(Guid.Parse("00002a19-0000-1000-8000-00805f9b34fb"));
            var bytes = new byte[] { 0x01 };
            await characteristic.WriteAsync(bytes);
            Console.WriteLine("Write successful.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing characteristic: {ex.Message}");
        }
    }
}
