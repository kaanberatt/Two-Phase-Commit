using Coordinator.Enums;

namespace Coordinator.Models;

public record NodeState(Guid TransactionId)
{
    public Guid Id { get; set; }

    // 1. aşamanın durumunu ifade ediyor.
    public ReadyType IsReady { get; set; }

    // 2. aşamanın sonucunda işlemin başarılı olup olmadığını ifade ediyor.
    public TransactionState TransactionState { get; set; }
    public Node Node  { get; set; }
}
