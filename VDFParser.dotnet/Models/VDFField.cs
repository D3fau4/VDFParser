using System;

namespace VDFParser.Models
{
    [AttributeUsage(AttributeTargets.Property)]
    public class VDFField : Attribute
    {
        public readonly string Name;

        public VDFField(string name)
        {
            Name = name;
            Type = VDFFieldType.String;
        }

        public VDFFieldType Type { get; set; }
    }
}