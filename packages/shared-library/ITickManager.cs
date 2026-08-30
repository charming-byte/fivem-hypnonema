using System;
using System.Threading.Tasks;

namespace Hypnonema.Shared;

public interface ITickManager
{
    void Add(Func<Task> func);

    Task<bool> RunAsync(Func<Task> func);

    void Remove(Func<Task> func);
}