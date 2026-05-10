using System.Reflection;
using Archon.Core.Entities;

namespace AgencyCampaign.Testing.TestSupport
{
    public static class EntityIdSetter
    {
        public static T WithId<T>(this T entity, long id) where T : Entity
        {
            ArgumentNullException.ThrowIfNull(entity);

            PropertyInfo property = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public)!;
            property.SetValue(entity, id);
            return entity;
        }
    }
}
