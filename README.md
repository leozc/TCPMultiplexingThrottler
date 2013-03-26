TCPMultiplexingThrottler
========================
C# Sample implementation for Multiplexing Throttler.
Please see https://github.com/leozc/TCPMulportsServer for a multiport server with speed metrics, which is a good tool for testing with this project.

## Goal
This tool can help distribute a large buffer to multiple TCP endpoint concurrently and respecting the rate given.

## Usage
### API
    <summary>
    Construct a throttler
    </summary>
    <param name="destinations">a list of address in the format of ip:   port string</param>
    <param name="speedInBps">integer indicate the corresponding speed   in destinations</param>
    param name="content">the file represented in byte array</param>
    public MultiplexThrottler(IList<String> destinations, IList<int> speedInBps, Byte[] content)'

### Main
A very simple main exe for demostration purpose, see the sample in Program.cs, and change it when needed.
    EXEC SampleInput.txt SampleData
	
Tips: use this command to generate an exact 64 MB files.
    fsutil file createnew sampledata 67108864'
## Design
### Pluginable Design
As the nature of problem, it is arguably impossible to write an algorithm does optimal throttling in all aspects, so it is critical to have a flexible framework and design to enable low-overhead changes, fast experiment as well as a good seperation among the data delivery, devices management and data throttling policy 
### Event Driven -  Almost :-)
The impmentation is based on asynchrous TCP IO (BeginSend but not SendAsync, SendAsync is better perf though...).
Low threading overhead: it has two user threads, one for main thread(which mostly waiting for completion), and another for deferred socket delivery delivery. 
### Main Classes/Interfaces
IThrottlerPoclicyHandler: The throttler interfaces, implement DispatchOneDataCycle and SendCompleteHandler function to control the throttling rate with the help of the info deviceManager provides.
    void SendCompleteHandler(IAsyncResult deviceManager);
	void DispatchOneDataCycle(IDeviceManager deviceManager);
IDeviceManager: the abstraction object that manages the connection of one remote device and providing data delivery methods as well corresponding metrics.
MultiplexThrottler: Manage devices, dispatch traffic and provide mutex to wait for state changes of devices.

### Some concrete classes
#### Throttlers
>1. UnlimitedThrottlerPolicyHandler.cs : unlimited rate throttler, good for testing purpose.
>2. SimplePerDataBlockThrottlerPolicyHandler.cs: constant rate throttler around ~95% of targeted rate for 64 clients on local loop
>3. CatchupThrottlerPolicyHandler..cs: constant rate throttler with burst, around ~99% of targeted rate for 64 clients on local loop
#### DeviceManager
>1. DeviceManager.cs : main device manager
