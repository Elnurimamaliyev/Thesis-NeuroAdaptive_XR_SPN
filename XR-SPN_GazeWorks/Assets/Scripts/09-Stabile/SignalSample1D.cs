using System;

public class SignalSample1D : ICloneable
{

    public double time;
    public double timeLsl;
    public float[] values;

    public SignalSample1D()
    {
    }

    public SignalSample1D(double lastTimeStamp, double timeLsl,  float[] samples)
    {
        this.time = lastTimeStamp;
        this.timeLsl = timeLsl;
        this.values = samples;
    }


    public SignalSample1D(double time, float[] samples)
    {
        this.time = time;
        this.values = samples;
    }


    #region Properties
    public double Time
    {
        get
        {
            return this.time;
        }

        set
        {
            this.time = value;
        }
    }

    public float[] Values
    {
        get
        {
            return this.values;
        }

        set
        {
            this.values = value;
        }
    }
    public float SignalValue {
        get
        {
            return this.values[0];
        }
    }

    #endregion Properties


    public object Clone()
    {
        return new SignalSample1D(this.time, this.timeLsl, this.values);
    }

}
