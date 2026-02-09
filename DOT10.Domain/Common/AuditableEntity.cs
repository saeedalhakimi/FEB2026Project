using System;
using System.Collections.Generic;
using System.Text;

namespace DOT10.Domain.Common
{
    public abstract class AuditableEntity
    {
        public DateTime CreatedAtUtc { get; protected set; }
        public DateTime? UpdatedAtUtc { get; protected set; }

        protected void Touch()
        {
            UpdatedAtUtc = DateTime.UtcNow;
        }

        protected void SetCreated()
        {
            CreatedAtUtc = DateTime.UtcNow;
        }
    }
}
