﻿using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.Cms.Core.Webhooks.Events.Media;

public class MediaTypeChangedWebhookEvent : WebhookEventBase<MediaTypeChangedNotification>
{
    public MediaTypeChangedWebhookEvent(
        IWebhookFiringService webhookFiringService,
        IWebHookService webHookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor)
        : base(webhookFiringService, webHookService, webhookSettings, serverRoleAccessor, "Media Type Changed")
    {
    }
}
