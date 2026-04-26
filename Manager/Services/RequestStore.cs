using System.Collections.Concurrent;
using Manager.Models;

namespace Manager.Services;

public class RequestStore
{
    private readonly ConcurrentDictionary<Guid, CrackRequestState> _requests = new();
    
    public void Add(CrackRequestState state) => _requests[state.RequestId] = state;
    
    public bool TryGet(Guid requestId, out CrackRequestState? state) 
        => _requests.TryGetValue(requestId, out state);
    
    public void Update(Guid requestId, Action<CrackRequestState> update)
    {
        if (_requests.TryGetValue(requestId, out var state))
        {
            lock (state)
            {
                update(state);
            }
        }
    }
}