using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace BackBuddy.Api.Service.V1.Database.MongoDB
{
    public class GuidRepresentationConvention : ConventionBase, IMemberMapConvention
    {
        public void Apply(BsonMemberMap memberMap)
        {
            if (memberMap.MemberType == typeof(Guid))
            {
                memberMap.SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            }
            else if (memberMap.MemberType == typeof(Guid?))
            {
                memberMap.SetSerializer(new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));
            }
            else if (memberMap.MemberType == typeof(List<Guid>))
            {
                memberMap.SetSerializer(new EnumerableInterfaceImplementerSerializer<List<Guid>>(new GuidSerializer(GuidRepresentation.Standard)));
            }
        }
    }
}
