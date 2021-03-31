# Project JetHub

This is a project for judgehost monitor, which exposes the basic information of the judgehost machine and provides the ability to read judgehost local files without logging into the machine.

Probably this can be extended to the next datacenter management tool?

### Features

- Representing system status like processor, memory usage, disk usage, kernel, cmdline
- Loading packages installed on this Ubuntu system
- Exposing the directories under `/opt/domjudge/judgehost/judgings/` to review files that are not transferred back to the contest control system
- Reading `/opt/domjudge/judgehost/etc/restapi.secret`

### Inspired by

- [DOMjudge judgehost](https://github.com/domjudge/domjudge)
- [KuduLite](https://github.com/Azure-App-Service/KuduLite)
- [JSON Tree Viewer](https://github.com/summerstyle/jsonTreeViewer)
- [PowerShell Core](https://github.com/powershell/powershell)

