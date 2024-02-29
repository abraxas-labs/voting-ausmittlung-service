// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using MassTransit;
using Voting.Ausmittlung.Core.Services;

namespace Voting.Ausmittlung.Core.Messaging;

/// <summary>
/// A scoped proxy to forward messages to the message consumer hub (singleton).
/// (required since we need a singleton to broadcast to the listeners, but mass transit consumers are scoped).
/// </summary>
/// <typeparam name="TMessage">The type of the message.</typeparam>
/// <typeparam name="TListenerMessage">The type of the listener data message.</typeparam>
public abstract class LanguageAwareMessageConsumer<TMessage, TListenerMessage> : IConsumer<TMessage>
    where TMessage : class
    where TListenerMessage : class
{
    private readonly LanguageAwareMessageConsumerHub<TMessage, TListenerMessage> _hub;
    private readonly LanguageService _languageService;

    protected LanguageAwareMessageConsumer(LanguageAwareMessageConsumerHub<TMessage, TListenerMessage> hub, LanguageService languageService)
    {
        _hub = hub;
        _languageService = languageService;
    }

    public Task Consume(ConsumeContext<TMessage> context)
    {
        return _hub.Consume(context.Message, lang =>
        {
            _languageService.SetLanguage(lang);
            return Transform(context.Message);
        });
    }

    protected abstract Task<TListenerMessage?> Transform(TMessage message);
}
