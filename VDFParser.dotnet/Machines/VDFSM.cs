﻿using System;
using System.Collections.Generic;
using System.Linq;
using VDFParser.Models;

namespace VDFParser.Machines
{
    /// <summary>
    ///     State machine used to parse a VDF structure
    /// </summary>
    public class VDFSM
    {
        private static readonly Type entryType = typeof(VDFEntry);
        private readonly VDFIndexedArraySM arraySM;
        private readonly VDFFieldsSM fieldsSM;

        private readonly List<byte> tmpBuffer;
        private VDFEntry currentEntry;

        private readonly List<VDFEntry> entries;

        private MainSMState mainState;

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:VDFParser.Machines.VDFSM" /> class.
        /// </summary>
        public VDFSM()
        {
            entries = new List<VDFEntry>();
            tmpBuffer = new List<byte>();
            mainState = MainSMState.IndexBeginIndicator;

            arraySM = new VDFIndexedArraySM();
            fieldsSM = new VDFFieldsSM();
        }

        /// <summary>
        ///     Gets the parsed entries
        /// </summary>
        /// <value>The entries.</value>
        public VDFEntry[] Entries => entries.ToArray();

        /// <summary>
        ///     Flushes any remaining entry to the result array
        /// </summary>
        public void Flush()
        {
            if (currentEntry != null)
            {
                Console.WriteLine("Parsed " + currentEntry);
                entries.Add(currentEntry);
            }

            currentEntry = null;
        }

        /// <summary>
        ///     Feeds a given byte to the SM
        /// </summary>
        /// <param name="b">Incoming byte to be fed to the SM</param>
        public void Feed(byte b)
        {
            switch (mainState)
            {
                case MainSMState.IndexBeginIndicator:
                    if (b == 0x00)
                    {
                        Flush();
                        mainState = MainSMState.IndexValue;
                        tmpBuffer.Clear();
                        fieldsSM.Reset();
                    }

                    break;
                case MainSMState.IndexValue:
                    if (b == 0x00)
                    {
                        // Okay, next would be IndexEndIndicator, 
                        // but the terminator is also the separator, 
                        // and things would get messy. Let's just
                        // skip a whole SM step here and pretend
                        // everything is fine.
                        int indexResult;
                        if (int.TryParse(tmpBuffer.StringFromByteArray(), out indexResult))
                        {
                            currentEntry = new VDFEntry();
                            currentEntry.Index = indexResult;
                            mainState = MainSMState.Fields;
                        }

                        // The next is necessary to satisfy the SM pattern.
                        fieldsSM.Feed(b);
                        break;
                    }

                    tmpBuffer.Add(b);
                    break;
                case MainSMState.Fields:
                    if (fieldsSM.Feed(b))
                    {
                        foreach (var k in fieldsSM.Fields) FillEntry(k.Key, k.Value);
                        mainState = MainSMState.IndexBeginIndicator;
                    }

                    break;
            }
        }


        private void FillEntry(string fieldName, byte[] value)
        {
            var props = from p in entryType.GetProperties()
                where Attribute.IsDefined(p, typeof(VDFField))
                where p.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)
                select p;
            var prop = props.FirstOrDefault();
            if (prop == null) return;

            object result = null;
            if (prop.PropertyType == typeof(int))
            {
                result = BitConverter.ToInt32(value, 0);
            }
            else if (prop.PropertyType == typeof(string))
            {
                result = value.StringFromByteArray();
            }
            else if (prop.PropertyType.IsArray && prop.PropertyType.GetElementType() == typeof(string))
            {
                result = parseIndexedArray(value);
            }
            else
            {
                Console.WriteLine("Unable to deserialize field '" + fieldName + "': Not Implemented.");
                return;
            }

            prop.SetValue(currentEntry, result);
        }

        private string[] parseIndexedArray(byte[] input)
        {
            if (input.Length < 5) return new string[] { };
            arraySM.Reset();
            arraySM.Feed(input);
            arraySM.Flush();
            return arraySM.ParsedArray;
        }

        private enum MainSMState
        {
            IndexBeginIndicator, // 0x00
            IndexValue, // [^0x00]+
            IndexEndIndicator, // 0x00
            Fields // 0x00
        }
    }
}