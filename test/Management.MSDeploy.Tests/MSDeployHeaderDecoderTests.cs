namespace Xylab.Management.WebDeploy.UnitTests;

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class MSDeployHeaderDecoderTests
{
    private const string ProviderOptions =
        "H4sIAAAAAAAEAE2PwU7DMAyGW1gjVeOyN+gDVNG2boNLT3BDjB0QHLZL2noiIourxFm1pwcHVQMffv+ybH92kiZJ8s0Rc4y7G5b9i24dejySHKCRHfQGLyewVBbv4LxGW9/LhZzLeVk8BkPBQW0hkFOmLHahMbp9hssbfoGtq0VzrB7WG9VVmxVU6yySln+ADwY8XQH/7M7hWXfgXntioJ/E4/qxtlUnmPSKPjPLzouzMgF8mgoRzxe3LNMWLcU13CV+h70mOAzD4BApz7iSC5ZZNn4+E6OZ/gDDKUfKFQEAAA==";

    private const string SyncOptions =
        "H4sIAAAAAAAEAIWUbW/TMBDHO2CV0BAIPkG111O30a3wYkWCPkAEa6tl3SbxynUu1Krjs87ORvaB+JpwSaYyVpf5RRLlfr77+x7c2Go0Gr95le9yvXjCj++nShI6TH37BubtBKzGIgPj91oXQE6h6b1rH7YP2gd7rX6ufU7QM5B7EnqvNc3nWsmvUJzjEkyvczhPO++PuyLpdI+gc7xdRmr/DXDJAQarAPc+48LIifUczDV5S/NmIXyU7uQO+guQS5dnrxPQ4GEAzisjSvKVsDb2SDA0Yq4h2aZcg3uTgNSCIJkKEhnvIFcedata5XlZ0lbzKX/8en5S7fhwApWDlmG+tzs014rQlKouBKnSMkbKhFa3sNvaf0CvoqjbStQ68XGOI6WZCJgMmiLD3M1c0GztFFFHCUtRvggCZ4j+P/I+aZTLL4KyNNeDKoETC1QpdRvomXG5tUgeksd39LVwTsk7pVNCD9KfcVYDKIHwwCQ3TOVuE4bOTbXwKZ8qjERR3EeTqh8jwqx7dI6dt+vQKXixLPuS6e66OZYLyETMaoNFi5fKRuaas5rEmJMMqCiR2bgfMHAvfwbDSdNh49V0/f+MdCyFqeNKzKzw69CVjZywKlCH6IfhQeA+g2/C+UtSHs5VFlDNIweGG0oW1WDdFW74UzkfcPuAjgw36qPUSCg9MUMirHs6Ue4+jGP0dWOtqvsvMTSSCuv5rqirHEDq49bmyCx4+rwwcpND7oC63AOVpkCwmSzTP4YboDKTbgN0KciMkO5k8pQILzagnN5JmmplQsZg2krnytRn3q+vp+YzvqxentjVfca2nT+9JkGJzAUAAA==";

    [TestMethod]
    public void DecodesProviderOptionsWithoutLoadingSerializedTypes()
    {
        var options = MSDeployHeaderDecoder.DecodeProviderOptions(ProviderOptions);

        Assert.AreEqual("contentPath", options.ProviderName);
        Assert.AreEqual("site\\wwwroot", options.Path);
    }

    [TestMethod]
    public void DecodesWhatIfSyncOptionsWithoutBinaryFormatter()
    {
        var options = MSDeployHeaderDecoder.DecodeSyncOptions(SyncOptions);

        Assert.IsTrue(options.WhatIf);
        Assert.IsFalse(options.DeleteDestination);
    }

    [TestMethod]
    public void RejectsInvalidBase64()
    {
        Assert.ThrowsExactly<InvalidDataException>(
            () => MSDeployHeaderDecoder.DecodeProviderOptions("not-base64"));
    }

    [TestMethod]
    public void RejectsTruncatedGzip()
    {
        Assert.ThrowsExactly<InvalidDataException>(
            () => MSDeployHeaderDecoder.DecodeProviderOptions("H4sIAAAAAA=="));
    }
}
