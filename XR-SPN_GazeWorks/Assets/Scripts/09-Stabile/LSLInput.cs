using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL;
using static LSL.liblsl;

public class LSLInput : MonoBehaviour
{
    public string inputName = "EEG";  // Default to EEG
    public LSLType StreamType = LSLType.type;
    public string StreamValue = "EEG";
    public double streamTimeout = 0.0;
    public int streamMinimum = 1;
    public float scaleInput = 0.1f;
    liblsl.StreamInfo[] streamInfos;
    liblsl.StreamInlet streamInlet;
    public double lastTimeStamp;
    public double unixTime = 0;
    public double time;
    public double timeLsl;

    public float[] lastValues;

    [Range(1, 3000)]
    public int bufferCount = 10;
    private float[,] rawBufferedValues;
    private double[] rawBufferedTimes;

    public LimitedSizeList<SignalSample1D> samples;
    public int samplesTotal;
    public int samplesBuffer = 5000;
    
    public string InfoHostName; //Hostname of the providing machine
    public string InfoType;
    public string InfoName;
    public string InfoChannelFormat; //Format/type of each channel
    public int    InfoChannelCount; //Number of channels per sample. 
    public double InfoNominalSrate; //The sampling rate (in Hz) as by the data source
    public DataLogger logger;

    // Start is called before the first frame update
    void Start()
    {
        samples = new LimitedSizeList<SignalSample1D>(samplesBuffer);
        
        // Add auto-detection of DataLogger to avoid manual linking
        if (logger == null)
        {
            logger = DataLogger.Instance;
            if (logger == null)
                Debug.LogError($"LSLInput '{gameObject.name}' could not find a DataLogger instance!");
            else
                Debug.Log($"LSLInput '{gameObject.name}' auto-connected to DataLogger");
        }
    }

    // Update is called once per frame
    void Update()
    {
        unixTime = UnixTime.GetTime();
        if (streamInlet == null)
        {
            // Try to resolve the stream
            streamInfos = liblsl.resolve_stream(StreamType.ToString(), StreamValue, streamMinimum, streamTimeout);
            
            // If no stream found with the current settings, try to find any EEG stream
            if (streamInfos.Length == 0 && StreamValue == "EEG")
            {
                Debug.Log("No standard 'EEG' stream found, looking for any EEG device...");
                streamInfos = liblsl.resolve_stream("type", "EEG");
            }
            
            if (streamInfos.Length > 0)
            {
                streamInlet = new liblsl.StreamInlet(streamInfos[0]);

                InfoChannelCount = streamInlet.info().channel_count();
                InfoChannelFormat = convert(streamInlet.info().channel_format());
                InfoType = streamInlet.info().type();

                InfoName = streamInlet.info().name();
                InfoNominalSrate = streamInlet.info().nominal_srate();
                InfoHostName = streamInlet.info().hostname();
                

                // Important: Update inputName to match the actual stream name
                // This will ensure DataLogger accepts it even if it's a LiveAmp device
                inputName = InfoName;
                Debug.Log($"Connected to LSL stream: {inputName} ({InfoType}) with {InfoChannelCount} channels at {InfoNominalSrate}Hz");
                lastValues = new float[InfoChannelCount];
                rawBufferedValues = new float[bufferCount, InfoChannelCount];
                rawBufferedTimes = new double[bufferCount];
                
                streamInlet.open_stream();
            }
        }

        if (streamInlet != null)
        {
            // Pull data from the stream
            int count = streamInlet.pull_chunk(rawBufferedValues, rawBufferedTimes);
            SignalSample1D newSample;
            for (int j = 0; j < count; j++)
            {
                lastTimeStamp = rawBufferedTimes[j];
                for (int i = 0; i < InfoChannelCount; i++)
                {
                    lastValues[i] = rawBufferedValues[j, i];
                }
                newSample = new SignalSample1D(unixTime, lastTimeStamp, lastValues);
                
                // Protect against null logger
                if (logger != null)
                    logger.write(inputName, newSample);
                else 
                    Debug.LogWarning("Cannot log LSL data: logger is null");
                    
                if (samples == null)
                    samples = new LimitedSizeList<SignalSample1D>(samplesBuffer);
                samples.Add(newSample);
                samplesTotal = samples.Count;
            }
        }
    }

    private string convert(channel_format_t t) {
        switch (t)
        {
            case channel_format_t.cf_float32:
                return "Float32";
            case channel_format_t.cf_double64:
                return "Double64";
            case channel_format_t.cf_string:
                return "String";
            case channel_format_t.cf_int64:
                return "Int64";
            case channel_format_t.cf_int32:
                return "Int32";
            case channel_format_t.cf_int16:
                return "Int16";
            case channel_format_t.cf_int8:
                return "Int8";
            case channel_format_t.cf_undefined:
                return "Undefined";
            default:
                return "None";
        }
    }

    public enum LSLType
    {
        type, name, source_id //"desc/manufaturer"
    }
}
