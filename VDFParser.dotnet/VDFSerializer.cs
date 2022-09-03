using System;
using System.IO;
using System.Linq;
using VDFParser.Models;

namespace VDFParser
{
    /// <summary>
    ///     Provides facilities for writing back a VDF file from a given set of <see cref="T:VDFParser.Models.VDFEntry" />
    /// </summary>
    public static class VDFSerializer
    {
        private const byte PROPTYPE_STRING = 0x01;
        private const byte PROPTYPE_INTEGER = 0x02;
        private const byte PROPTYPE_LIST = 0x00;
        private const byte ENTRY_SEPARATOR = 0x08;

        private static readonly Type eType = typeof(VDFEntry);

        /// <summary>
        ///     Serialize the specified entries.
        /// </summary>
        /// <param name="entries">Entries to be serialized</param>
        public static byte[] Serialize(VDFEntry[] entries)
        {
            var index = 0;
            var props = from prop in eType.GetProperties()
                where Attribute.IsDefined(prop, typeof(VDFField))
                select prop;


            var s = new MemoryStream();
            s.Write(Shared.VDFHeader, 0, Shared.VDFHeader.Length);
            var w = new GenericWriter(s);
            foreach (var e in entries)
            {
                w.Nil();
                w.Write((index++).ToString());
                w.Nil();
                foreach (var p in props)
                {
                    var typeId = PROPTYPE_LIST;
                    var fieldMeta = (VDFField)Attribute.GetCustomAttribute(p, typeof(VDFField));
                    if (p.PropertyType == typeof(string))
                        typeId = PROPTYPE_STRING;
                    else if (p.PropertyType == typeof(int)) typeId = PROPTYPE_INTEGER;

                    w.Write(typeId);
                    w.Write(fieldMeta.Name);
                    w.Nil();
                    if (typeId == PROPTYPE_STRING)
                    {
                        var data = (string)p.GetValue(e);
                        if (data != null) w.Write((string)p.GetValue(e));
                        w.Nil();
                    }
                    else if (typeId == PROPTYPE_INTEGER)
                    {
                        w.Write((int)p.GetValue(e));
                    }
                    else
                    {
                        SerializeIndexedArray((string[])p.GetValue(e), w);
                    }
                }

                w.Write(new[] { ENTRY_SEPARATOR, ENTRY_SEPARATOR });
            }

            w.Write(new[] { ENTRY_SEPARATOR, ENTRY_SEPARATOR });
            return s.ToArray();
        }

        private static void SerializeIndexedArray(string[] arr, GenericWriter w)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                w.Write(PROPTYPE_STRING);
                w.Write(i.ToString());
                w.Nil();
                w.Write(arr[i]);
                w.Nil();
            }
        }
    }
}