﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;

namespace AllReady.Features.Notifications
{
    public class NotifyVolunteerForEventSignup : IAsyncNotificationHandler<VolunteerSignupNotification>
    {
        private readonly IMediator _mediator;
        private readonly IOptions<GeneralSettings> _options;

        public NotifyVolunteerForEventSignup(IMediator mediator, IOptions<GeneralSettings> options)
        {
            _mediator = mediator;
            _options = options;
        }

        public async Task Handle(VolunteerSignupNotification notification)
        {
            var model = await _mediator.SendAsync(new EventDetailForNotificationQueryAsync {EventId = notification.EventId, UserId = notification.UserId})
                .ConfigureAwait(false);

            var signup = model.UsersSignedUp?.FirstOrDefault(s => s.User.Id == notification.UserId);
            if (signup == null)
            {
                return;
            }

            var emailRecipient = !string.IsNullOrWhiteSpace(signup.PreferredEmail)
                ? signup.PreferredEmail
                : signup.User?.Email;

            if (string.IsNullOrWhiteSpace(emailRecipient))
            {
                return;
            }

            var eventLink = $"View event: {_options.Value.SiteBaseUrl}Admin/Event/Details/{model.EventId}";
            var subject = "allReady Event Enrollment Confirmation";

            var message = new StringBuilder();
            message.AppendLine($"This is to confirm that you have volunteered to participate in the following event:");
            message.AppendLine();
            message.AppendLine($"   Campaign: {model.CampaignName}");
            message.AppendLine($"   Event: {model.EventName} ({eventLink})");
            message.AppendLine();
            message.AppendLine($"Thanks for volunteering. Your help is appreciated.");

            var command = new NotifyVolunteersCommand
            {
                ViewModel = new NotifyVolunteersViewModel
                {
                    EmailMessage = message.ToString(),
                    HtmlMessage = message.ToString(),
                    EmailRecipients = new List<string> { emailRecipient},
                    Subject = subject
                }
            };

            await _mediator.SendAsync(command).ConfigureAwait(false);
        }
    }
}