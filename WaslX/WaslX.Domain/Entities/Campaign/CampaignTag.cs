using WaslX.Domain.Common;

namespace WaslX.Domain.Entities
{
    public class CampaignTag
    {
        public int CampaignId { get; set; }
        public int TagId { get; set; }

        public Campaign Campaign { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
    }
}
