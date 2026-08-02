using System;
using System.Collections.Generic;
using System.Text;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Result>
    where TCommand : ICommand;


public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
