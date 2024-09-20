// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.Messaging;

/// <inheritdoc />
public class LanguageAwareMessageConsumerHub<TFilterMessage, TListenerMessage>
    : MessageConsumerHubBase<TFilterMessage, TListenerMessage>
    where TListenerMessage : class
{
    public LanguageAwareMessageConsumerHub(ILogger<MessageConsumerHub<TFilterMessage, TListenerMessage>> logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Adds a callback to the listeners.
    /// Method is blocking until the cancellation token is fired.
    /// </summary>
    /// <param name="filter">The filter, only messages where this callback returns true are passed to the listener.</param>
    /// <param name="listener">The delegate to callback on new events.</param>
    /// <param name="language">The language of the listener.</param>
    /// <param name="cancellationToken">The cancellation token which stops listening to events and returns the method.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation. Blocks until the cancellation token fires.</returns>
    public Task Listen(
        Predicate<TFilterMessage> filter,
        Func<TListenerMessage, Task> listener,
        string language,
        CancellationToken cancellationToken)
        => Listen(new MessageConsumerRegistration<TFilterMessage, TListenerMessage>(filter, listener, language), cancellationToken);

    internal async Task Consume(TFilterMessage message, Func<string, Task<TListenerMessage?>> messageLanguageConverter)
    {
        var callbackTasks = new List<Task>();
        var registrations = GetConsumers(message);
        foreach (var registrationGroup in registrations.GroupBy(r => r.Language))
        {
            // conversion needs to be done serially (uses same DataContext instance)
            // since there are only a few registered listeners per language for the same messages
            // additional concurrency doesn't make a lot of sense here.
            // callbacks can then be called concurrently.
            var consumerMessage = await messageLanguageConverter(registrationGroup.Key);
            if (consumerMessage == null)
            {
                continue;
            }

            var consumerTasks = registrationGroup.Select(r => r.Consume(consumerMessage));
            callbackTasks.AddRange(consumerTasks);
        }

        await Task.WhenAll(callbackTasks);
    }
}
