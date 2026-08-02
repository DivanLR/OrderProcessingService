using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Abstractions.Messaging;

public interface ICommand : ICommand<Result>;

public interface ICommand<TResponse>;
