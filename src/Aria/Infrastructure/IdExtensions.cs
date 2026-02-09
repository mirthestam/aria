using Aria.Core.Extraction;
using GLib;

namespace Aria.Infrastructure;

public static class IdExtensions
{
    extension(Id id)
    {
        public Variant ToVariant()
        {
            return Variant.NewString(id.ToString());
        }

        public Variant ToVariantArray()
        {
            return Variant.NewArray(VariantType.String, [id.ToVariant()]);
        }
    }
}