using System.Collections.Generic;
using ProtoBuf;

namespace LootFilter
{
    /// <summary>
    /// Sent client → server when the player saves filter changes from the GUI
    /// or when a settings toggle changes.
    /// </summary>
    [ProtoContract]
    public class FilterUpdatePacket
    {
        [ProtoMember(1)]
        public List<string> FilteredItemCodes { get; set; } = new List<string>();

        [ProtoMember(2)]
        public List<string> FilteredKeywords { get; set; } = new List<string>();

        [ProtoMember(3)]
        public bool AutoDropFiltered { get; set; }

        [ProtoMember(4)]
        public bool AllowlistMode { get; set; }

        [ProtoMember(5)]
        public bool CrouchBypassEnabled { get; set; } = true;

        [ProtoMember(6)]
        public List<AttributeRulePacket> FilteredAttributes { get; set; } = new List<AttributeRulePacket>();

        /// <summary>Snapshot the given config into a new packet.</summary>
        public static FilterUpdatePacket FromConfig(LootFilterConfig cfg)
        {
            var packet = new FilterUpdatePacket
            {
                FilteredItemCodes   = new List<string>(cfg.FilteredItemCodes),
                FilteredKeywords    = new List<string>(cfg.FilteredKeywords),
                AutoDropFiltered    = cfg.AutoDropFiltered,
                AllowlistMode       = cfg.AllowlistMode,
                CrouchBypassEnabled = cfg.CrouchBypassEnabled
            };

            if (cfg.FilteredAttributes != null)
            {
                for (int i = 0; i < cfg.FilteredAttributes.Count; i++)
                {
                    var r = cfg.FilteredAttributes[i];
                    if (r == null) continue;
                    packet.FilteredAttributes.Add(new AttributeRulePacket
                    {
                        Field     = r.Field,
                        Op        = (int)r.Op,
                        Threshold = r.Threshold,
                        Label     = r.Label ?? ""
                    });
                }
            }

            return packet;
        }

        /// <summary>Convert packet back to a fresh <see cref="LootFilterConfig"/>.</summary>
        public LootFilterConfig ToConfig()
        {
            var cfg = new LootFilterConfig
            {
                FilteredItemCodes   = new List<string>(FilteredItemCodes ?? new List<string>()),
                FilteredKeywords    = new List<string>(FilteredKeywords ?? new List<string>()),
                AutoDropFiltered    = AutoDropFiltered,
                AllowlistMode       = AllowlistMode,
                CrouchBypassEnabled = CrouchBypassEnabled
            };

            if (FilteredAttributes != null)
            {
                for (int i = 0; i < FilteredAttributes.Count; i++)
                {
                    var r = FilteredAttributes[i];
                    if (r == null) continue;
                    cfg.FilteredAttributes.Add(new AttributeRule
                    {
                        Field     = r.Field ?? "",
                        Op        = (AttributeOperator)r.Op,
                        Threshold = r.Threshold,
                        Label     = r.Label ?? ""
                    });
                }
            }

            return cfg;
        }
    }

    /// <summary>
    /// Sent server → client to synchronise the authoritative config.
    /// Delivered on player join and after every confirmed <see cref="FilterUpdatePacket"/>.
    /// </summary>
    [ProtoContract]
    public class FilterSyncPacket
    {
        [ProtoMember(1)]
        public List<string> FilteredItemCodes { get; set; } = new List<string>();

        [ProtoMember(2)]
        public List<string> FilteredKeywords { get; set; } = new List<string>();

        [ProtoMember(3)]
        public bool AutoDropFiltered { get; set; }

        [ProtoMember(4)]
        public bool AllowlistMode { get; set; }

        [ProtoMember(5)]
        public bool CrouchBypassEnabled { get; set; } = true;

        [ProtoMember(6)]
        public List<AttributeRulePacket> FilteredAttributes { get; set; } = new List<AttributeRulePacket>();

        /// <summary>Snapshot the given config into a new packet.</summary>
        public static FilterSyncPacket FromConfig(LootFilterConfig cfg)
        {
            var packet = new FilterSyncPacket
            {
                FilteredItemCodes   = new List<string>(cfg.FilteredItemCodes),
                FilteredKeywords    = new List<string>(cfg.FilteredKeywords),
                AutoDropFiltered    = cfg.AutoDropFiltered,
                AllowlistMode       = cfg.AllowlistMode,
                CrouchBypassEnabled = cfg.CrouchBypassEnabled
            };

            if (cfg.FilteredAttributes != null)
            {
                for (int i = 0; i < cfg.FilteredAttributes.Count; i++)
                {
                    var r = cfg.FilteredAttributes[i];
                    if (r == null) continue;
                    packet.FilteredAttributes.Add(new AttributeRulePacket
                    {
                        Field     = r.Field,
                        Op        = (int)r.Op,
                        Threshold = r.Threshold,
                        Label     = r.Label ?? ""
                    });
                }
            }

            return packet;
        }

        /// <summary>Convert packet back to a fresh <see cref="LootFilterConfig"/>.</summary>
        public LootFilterConfig ToConfig()
        {
            var cfg = new LootFilterConfig
            {
                FilteredItemCodes   = new List<string>(FilteredItemCodes ?? new List<string>()),
                FilteredKeywords    = new List<string>(FilteredKeywords ?? new List<string>()),
                AutoDropFiltered    = AutoDropFiltered,
                AllowlistMode       = AllowlistMode,
                CrouchBypassEnabled = CrouchBypassEnabled
            };

            if (FilteredAttributes != null)
            {
                for (int i = 0; i < FilteredAttributes.Count; i++)
                {
                    var r = FilteredAttributes[i];
                    if (r == null) continue;
                    cfg.FilteredAttributes.Add(new AttributeRule
                    {
                        Field     = r.Field ?? "",
                        Op        = (AttributeOperator)r.Op,
                        Threshold = r.Threshold,
                        Label     = r.Label ?? ""
                    });
                }
            }

            return cfg;
        }
    }

    /// <summary>
    /// Protobuf-serializable representation of a single <see cref="AttributeRule"/>.
    /// Using a flat DTO avoids protobuf-net's restrictions on polymorphic enums and
    /// keeps the wire format simple.
    /// </summary>
    [ProtoContract]
    public class AttributeRulePacket
    {
        [ProtoMember(1)] public string Field     { get; set; } = "";
        [ProtoMember(2)] public int    Op        { get; set; }
        [ProtoMember(3)] public double Threshold { get; set; }
        [ProtoMember(4)] public string Label     { get; set; } = "";
    }
}
