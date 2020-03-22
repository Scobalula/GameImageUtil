using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

// NOTE: This is a basic port of Porter's cast.py with just enough to parse materials, it is not recommended to use it

namespace CoDImageUtil
{
    class CastException : Exception
    {
        public CastException()
        {
        }

        public CastException(string message)
            : base(message)
        {
        }

        public CastException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Cast Property Types
    /// </summary>
    enum CastPropertyType : ushort
    {
        /// <summary>
        /// 8-Bit Integer (uint8_t/byte)
        /// </summary>
        Byte = 'b',

        /// <summary>
        /// 16-Bit Integer (uint16_t/ushort)
        /// </summary>
        Short = 'h',

        /// <summary>
        /// 32-Bit Integer (uint32_t/uint)
        /// </summary>
        Integer32 = 'i',

        /// <summary>
        /// 64-Bit Integer (uint64_t/ulong)
        /// </summary>
        Integer64 = 'l',

        /// <summary>
        /// Single Precision Value (float)
        /// </summary>
        Float = 'f',

        /// <summary>
        /// Double Precision Value (double)
        /// </summary>
        Double = 'd',

        /// <summary>
        /// Null terminated UTF-8 string
        /// </summary>
        String = 's',

        /// <summary>
        /// Float precision vector XYZ
        /// </summary>
        Vector2 = 0x7632,

        /// <summary>
        /// Float precision vector XYZ
        /// </summary>
        Vector3 = 0x7633,

        /// <summary>
        /// Float precision vector XYZW
        /// </summary>
        Vector4 = 0x7634
    }

    enum CastIdentifier : uint
    {
        Root = 0x746F6F72,
        Model = 0x6C646F6D,
        Mesh = 0x6873656D,
        Skeleton = 0x6C656B73,
        Bone = 0x656E6F62,
        Animation = 0x6D696E61,
        Curve = 0x76727563,
        NotificationTrack = 0x6669746E,
        Material = 0x6C74616D,
        File = 0x656C6966,
    };

    class CastProperty
    {
        /// <summary>
        /// Gets or Sets the Name of the Property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets the Property Type
        /// </summary>
        public CastPropertyType Type { get; set; }

        /// <summary>
        /// Gets or Sets the Values
        /// </summary>
        public object Values { get; set; }

        /// <summary>
        /// Initializes a new instance of the CastProperty class
        /// </summary>
        public CastProperty() { }

        /// <summary>
        /// Initializes a new instance of the CastProperty class and loads the property from the given reader
        /// </summary>
        /// <param name="reader">Reader to load from</param>
        public CastProperty(BinaryReader reader)
        {
            Load(reader);
        }

        /// <summary>
        /// Loads the property from the given reader
        /// </summary>
        /// <param name="reader">Reader to load from</param>
        public void Load(BinaryReader reader)
        {
            var type = reader.ReadUInt16();
            var nameSize = reader.ReadUInt16();
            var valueCount = reader.ReadInt32();

            Name = new string(reader.ReadChars(nameSize));
            Type = (CastPropertyType)type;

            switch (Type)
            {
                case CastPropertyType.Byte:
                    {
                        Values = reader.ReadBytes(valueCount);
                        break;
                    }
                case CastPropertyType.Short:
                    {
                        Values = new ushort[valueCount];
                        Buffer.BlockCopy(reader.ReadBytes(valueCount * 2), 0, (ushort[])Values, 0, valueCount * 2);
                        break;
                    }
                case CastPropertyType.Integer32:
                    {
                        Values = new uint[valueCount];
                        Buffer.BlockCopy(reader.ReadBytes(valueCount * 4), 0, (uint[])Values, 0, valueCount * 4);
                        break;
                    }
                case CastPropertyType.Integer64:
                    {
                        Values = new ulong[valueCount];
                        Buffer.BlockCopy(reader.ReadBytes(valueCount * 8), 0, (ulong[])Values, 0, valueCount * 8);
                        break;
                    }
                case CastPropertyType.Float:
                    {
                        Values = new float[valueCount];
                        Buffer.BlockCopy(reader.ReadBytes(valueCount * 4), 0, (float[])Values, 0, valueCount * 4);
                        break;
                    }
                case CastPropertyType.Double:
                    {
                        Values = new double[valueCount];
                        Buffer.BlockCopy(reader.ReadBytes(valueCount * 8), 0, (double[])Values, 0, valueCount * 8);
                        break;
                    }
                case CastPropertyType.Vector2:
                    {
                        Values = new float[valueCount * 2];
                        Buffer.BlockCopy(reader.ReadBytes(valueCount * 8), 0, (float[])Values, 0, valueCount * 8);
                        break;
                    }
                case CastPropertyType.Vector3:
                    {
                        Values = new float[valueCount * 3];
                        Buffer.BlockCopy(reader.ReadBytes(valueCount * 12), 0, (float[])Values, 0, valueCount * 12);
                        break;
                    }
                case CastPropertyType.Vector4:
                    {
                        Values = new float[valueCount * 4];
                        Buffer.BlockCopy(reader.ReadBytes(valueCount * 16), 0, (float[])Values, 0, valueCount * 16);
                        break;
                    }
                case CastPropertyType.String:
                    {
                        Values = new string[valueCount];

                        for (int i = 0; i < valueCount; i++)
                        {
                            StringBuilder str = new StringBuilder();
                            int byteRead;

                            while ((byteRead = reader.BaseStream.ReadByte()) != 0x0)
                                str.Append(Convert.ToChar(byteRead));

                            ((string[])Values)[i] = str.ToString();
                        }

                        break;
                    }
            }
        }
    }

    class CastModel : CastNode
    {
        /// <summary>
        /// Initializes a new instance of the CastModel class
        /// </summary>
        public CastModel() : base() { }

        /// <summary>
        /// Initializes a new instance of the CastModel class and loads the node from the given reader
        /// </summary>
        /// <param name="reader">Reader to load from</param>
        public CastModel(BinaryReader reader) : base(reader)
        {
            Load(reader);
        }

        /// <summary>
        /// Initializes a new instance of the CastModel class and copies from the base node
        /// </summary>
        /// <param name="baseNode">Node to copy from</param>
        public CastModel(CastNode baseNode)
        {
            ChildNodes = baseNode.ChildNodes;
            Properties = baseNode.Properties;
            Hash = baseNode.Hash;
            Identifier = baseNode.Identifier;
        }

        /// <summary>
        /// Gets the Model Materials
        /// </summary>
        public List<CastMaterial> Materials
        {
            get
            {
                return ChildrenOfType<CastMaterial>();
            }
        }
    }

    class CastFile : CastNode
    {
        /// <summary>
        /// Initializes a new instance of the CastFile class
        /// </summary>
        public CastFile() : base()
        {

        }

        /// <summary>
        /// Initializes a new instance of the CastFile class and loads the node from the given reader
        /// </summary>
        /// <param name="reader">Reader to load from</param>
        public CastFile(BinaryReader reader) : base(reader)
        {
            Load(reader);
        }

        /// <summary>
        /// Initializes a new instance of the CastFile class and copies from the base node
        /// </summary>
        /// <param name="baseNode">Node to copy from</param>
        public CastFile(CastNode baseNode)
        {
            ChildNodes = baseNode.ChildNodes;
            Properties = baseNode.Properties;
            Hash = baseNode.Hash;
            Identifier = baseNode.Identifier;
        }

        /// <summary>
        /// Gets the file path
        /// </summary>
        public string Path
        {
            get
            {
                if (Properties.TryGetValue("p", out var prop) && prop.Values is string[] str && str.Length > 0)
                {
                    return str[0];
                }
                else
                {
                    return "";
                }
            }
        }
    }

    class CastMaterial : CastNode
    {
        /// <summary>
        /// Initializes a new instance of the CastMaterial class
        /// </summary>
        public CastMaterial() : base() { }

        /// <summary>
        /// Initializes a new instance of the CastMaterial class and loads the node from the given reader
        /// </summary>
        /// <param name="reader">Reader to load from</param>
        public CastMaterial(BinaryReader reader) : base(reader)
        {
            Load(reader);
        }

        /// <summary>
        /// Initializes a new instance of the CastMaterial class and copies from the base node
        /// </summary>
        /// <param name="baseNode">Node to copy from</param>
        public CastMaterial(CastNode baseNode)
        {
            ChildNodes = baseNode.ChildNodes;
            Properties = baseNode.Properties;
            Hash = baseNode.Hash;
            Identifier = baseNode.Identifier;
        }

        /// <summary>
        /// Gets the name of the material
        /// </summary>
        public string Name
        {
            get
            {
                if (Properties.TryGetValue("n", out var prop) && prop.Values is string[] str && str.Length > 0)
                {
                    return str[0];
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Gets the type of the material
        /// </summary>
        public string Type
        {
            get
            {
                if (Properties.TryGetValue("t", out var prop) && prop.Values is string[] str && str.Length > 0)
                {
                    return str[0];
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Gets the type of the image slots for this material
        /// </summary>
        public Dictionary<string, CastNode> Slots
        {
            get
            {
                var results = new Dictionary<string, CastNode>();

                foreach (var prop in Properties)
                    if (prop.Value.Name != "n" && prop.Value.Name != "t")
                        if (prop.Value.Values is ulong[] hash && hash.Length > 0)
                            results[prop.Value.Name] = ChildByHash(hash[0]);

                return results;
            }
        }
    }

    class CastNode
    {
        /// <summary>
        /// Gets or Sets the Child Nodes
        /// </summary>
        public Dictionary<ulong, CastNode> ChildNodes { get; set; }

        /// <summary>
        /// Gets or Sets the Child Nodes
        /// </summary>
        public Dictionary<string, CastProperty> Properties { get; set; }

        /// <summary>
        /// Gets or Sets the Hash
        /// </summary>
        public ulong Hash { get; set; }

        /// <summary>
        /// Gets or Sets the Cast Identifier
        /// </summary>
        public CastIdentifier Identifier { get; set; }

        /// <summary>
        /// Initializes a new instance of the CastNode class
        /// </summary>
        public CastNode() { }

        /// <summary>
        /// Initializes a new instance of the CastNode class and loads the node from the given reader
        /// </summary>
        /// <param name="reader">Reader to load from</param>
        public CastNode(BinaryReader reader)
        {
            Load(reader);
        }

        /// <summary>
        /// Gets the node by type, if not found, null is returned
        /// </summary>
        /// <param name="hash">Type to search for</param>
        /// <returns>Node with the given id, otherwise null</returns>
        public Dictionary<ulong, CastNode> ChildrenOfType(CastIdentifier id)
        {
            var results = new Dictionary<ulong, CastNode>();

            foreach (var node in ChildNodes)
                if (node.Value.Identifier == id)
                    results[node.Key] = node.Value;

            return results;
        }

        /// <summary>
        /// Gets the node by type, if not found, null is returned
        /// </summary>
        /// <returns>Node with the given type, otherwise null</returns>
        public List<T> ChildrenOfType<T>() where T : CastNode
        {
            return ChildNodes.Where(x => x.Value.GetType() == typeof(T)).Select(x => x.Value).Cast<T>().ToList();
        }

        /// <summary>
        /// Gets the node by hash, if not found, null is returned
        /// </summary>
        /// <param name="hash">Hash to search for</param>
        /// <returns>Node with the given hash, otherwise null</returns>
        public CastNode ChildByHash(ulong hash)
        {
            return ChildNodes.TryGetValue(hash, out var node) ? node : null;
        }

        /// <summary>
        /// Loads the property from the given reader
        /// </summary>
        /// <param name="reader">Reader to load from</param>
        public void Load(BinaryReader reader)
        {
#if DEBUG
                var nodeBegin = reader.BaseStream.Position;
#endif
            Properties = new Dictionary<string, CastProperty>();
            ChildNodes = new Dictionary<ulong, CastNode>();

            var identifier = reader.ReadUInt32();
            var nodeSize = reader.ReadUInt32();
            var nodeHash = reader.ReadUInt64();
            var propCount = reader.ReadUInt32();
            var childCount = reader.ReadUInt32();

            Identifier = (CastIdentifier)identifier;
            Hash = nodeHash;

            for (uint i = 0; i < propCount; i++)
            {
                var prop = new CastProperty(reader);
                Properties[prop.Name] = prop;
            }

            for (uint i = 0; i < childCount; i++)
            {
                var node = new CastNode(reader);

                // Quick switch will do for the purpose of just getting materials
                switch (node.Identifier)
                {
                    case CastIdentifier.Material:
                        ChildNodes[node.Hash] = new CastMaterial(node);
                        break;
                    case CastIdentifier.Model:
                        ChildNodes[node.Hash] = new CastModel(node);
                        break;
                    case CastIdentifier.File:
                        ChildNodes[node.Hash] = new CastFile(node);
                        break;
                    default:
                        ChildNodes[node.Hash] = node;
                        break;
                }
            }

#if DEBUG
                if (reader.BaseStream.Position != nodeBegin + nodeSize)
                    throw new CastException("Invalid node size");
#endif
        }
    }

    class Cast
    {
        /// <summary>
        /// Gets or Sets the Root Nodes
        /// </summary>
        public Dictionary<ulong, CastNode> RootNodes { get; set; }

        public Cast() { }

        public void Load(string path)
        {
            RootNodes = new Dictionary<ulong, CastNode>();

            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                if (reader.ReadUInt32() != 0x74736163)
                    throw new CastException("Invalid cast file magic");

                var version = reader.ReadUInt32();
                var rootNodes = reader.ReadInt32();
                var flags = reader.ReadUInt32();

                for (int i = 0; i < rootNodes; i++)
                {
                    var node = new CastNode(reader);
                    RootNodes[node.Hash] = node;
                }
            }
        }

        public void Load(Stream stream)
        {

        }

        public void Load(BinaryReader reader)
        {
            if (reader.ReadUInt32() != 0x74736163)
                throw new CastException("Invalid cast file magic");

            var version = reader.ReadUInt32();
            var rootNodes = reader.ReadInt32();
            var flags = reader.ReadUInt32();

            for (int i = 0; i < rootNodes; i++)
            {
                var node = new CastNode(reader);
            }
        }
    }
}
