using Microsoft.Extensions.Localization;
using Moq;

namespace AgencyCampaign.Testing.TestSupport
{
    public static class LocalizerMock
    {
        public static IStringLocalizer<T> Create<T>()
        {
            Mock<IStringLocalizer<T>> mock = new();
            mock.Setup(item => item[It.IsAny<string>()])
                .Returns<string>(key => new LocalizedString(key, key, false));
            mock.Setup(item => item[It.IsAny<string>(), It.IsAny<object[]>()])
                .Returns<string, object[]>((key, args) => new LocalizedString(key, key, false));
            return mock.Object;
        }
    }
}
