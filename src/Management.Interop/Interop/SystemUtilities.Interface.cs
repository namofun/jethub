#nullable enable
using Xylab;

namespace Xylab.Management.Interop;

using Xylab.Management.SystemInformation;

public interface ISystemUtilities
{
    UserInformation? FindUser(uint uid);

    UserInformation? FindUser(string name);

    uint GetUserId();

    uint GetEffectiveUserId();

    uint GetGroupId();

    uint GetEffectiveGroupId();

    void ChangeMode(string filePath, uint mode);

    bool TryChangeMode(string filePath, uint mode);

    void SetUmask(uint cmask, out uint originalCmask);
}
