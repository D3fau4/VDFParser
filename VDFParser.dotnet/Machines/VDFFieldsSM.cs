using System.Collections.Generic;

namespace VDFParser.Machines
{
    /// <summary>
    ///     State machine used to parse fields out from a VDF structure
    /// </summary>
    public class VDFFieldsSM
    {
        private readonly List<byte> tmpBuffer;
        private byte lastByte;
        private State state;
        private string tmpFieldName;
        private byte tmpFieldType;

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:VDFParser.Machines.VDFFieldsSM" /> class.
        /// </summary>
        public VDFFieldsSM()
        {
            Fields = new Dictionary<string, byte[]>();
            tmpBuffer = new List<byte>();
        }

        /// <summary>
        ///     Gets the parsing result
        /// </summary>
        /// <value>Fields returned by the SM</value>
        public Dictionary<string, byte[]> Fields { get; }

        /// <summary>
        ///     Resets this instance, cleaning all parsed data and fields
        /// </summary>
        public void Reset()
        {
            Fields.Clear();
            state = State.BeginIndicator;
            tmpBuffer.Clear();
            lastByte = 0x00;
            tmpFieldType = 0x00;
            tmpFieldName = null;
        }

        /// <summary>
        ///     Feeds a given byte to the SM
        /// </summary>
        /// <param name="b">Incoming byte to be fed to the SM</param>
        public bool Feed(byte b)
        {
            switch (state)
            {
                case State.BeginIndicator:
                    if (b == 0x00)
                    {
                        state = State.Type;
                        tmpBuffer.Clear();
                    }

                    break;
                case State.Type:
                    if (b == 0x01 || b == 0x02 || b == 0x00)
                    {
                        tmpFieldType = b;
                        state = State.Name;
                    }
                    else
                    {
                        state = State.BeginIndicator;
                    }

                    break;
                case State.Name:
                    if (b == 0x00)
                    {
                        tmpFieldName = tmpBuffer.StringFromByteArray();
                        tmpBuffer.Clear();
                        state = State.Value;
                    }
                    else
                    {
                        tmpBuffer.Add(b);
                    }

                    break;
                case State.Value:
                    var vdfEnd = false;
                    if (b == 0x08 && lastByte == 0x08)
                    {
                        // This reaches the end of VDFEntry and the last interaction
                        // added an extra byte to the temp buffer. Pop it and
                        // move along.
                        tmpBuffer.RemoveAt(tmpBuffer.Count - 1);
                        vdfEnd = true;
                    }

                    if ((tmpFieldType == 0x02 && tmpBuffer.Count == 3) || (tmpFieldType == 0x01 && b == 0x00) ||
                        (tmpFieldType == 0x00 && b == 0x08 && lastByte == 0x08))
                    {
                        if (tmpFieldType == 0x02) tmpBuffer.Add(b);

                        if (!string.IsNullOrEmpty(tmpFieldName)) Fields.Add(tmpFieldName, tmpBuffer.ToArray());

                        tmpFieldName = null;
                        tmpBuffer.Clear();

                        if (vdfEnd)
                        {
                            state = State.BeginIndicator;
                            return true;
                        }

                        state = State.Type;
                    }
                    else
                    {
                        tmpBuffer.Add(b);
                    }

                    break;
            }

            lastByte = b;
            return false;
        }

        private enum State
        {
            BeginIndicator, // 0x00
            Type, // ATM, 0x00 || 0x01 || 0x02
            Name, // [^0x00]+
            Value // [^0x00]+
        }
    }
}