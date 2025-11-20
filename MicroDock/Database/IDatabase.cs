using MicroDock.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Database;

public interface IDatabase
{
    IDatabaseDto GetDto();
}
