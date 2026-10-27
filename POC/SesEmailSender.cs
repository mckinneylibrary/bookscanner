using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scanner
{
    public static class SesEmailSender
    {
        public static async Task<string> SendScannedDocumentAsync(
    string fromAddress,
    string recipientAddress,
    string subject,
    string messageBody,
    string attachmentPath)
        {
            if (!File.Exists(attachmentPath))
            {
                throw new FileNotFoundException(
                    "Scanned document not found.",
                    attachmentPath);
            }

            using (var email = new MimeMessage())
            {
                email.From.Add(MailboxAddress.Parse(fromAddress));
                email.To.Add(MailboxAddress.Parse(recipientAddress));
                email.Subject = subject;

                var body = new BodyBuilder
                {
                    TextBody = messageBody
                };

                body.Attachments.Add(attachmentPath);
                email.Body = body.ToMessageBody();

                using (var stream = new MemoryStream())
                {
                    await email.WriteToAsync(stream);
                    stream.Position = 0;

                    var credentials = new Amazon.Runtime.CredentialManagement
                        .CredentialProfileStoreChain();

                    if (!credentials.TryGetAWSCredentials(
                        "McKinneyLibraryScannerProfile",
                        out var awsCredentials))
                    {
                        Scanner.Logger.Error(
                            "AWS credentials profile was not found.");

                        return null;
                    }

                    using (var ses =
                        new AmazonSimpleEmailServiceClient(
                            awsCredentials,
                            RegionEndpoint.USEast1))
                    using (var cts =
                        new CancellationTokenSource(
                            TimeSpan.FromSeconds(30)))
                    {
                        var request = new SendRawEmailRequest
                        {
                            Source = fromAddress,
                            Destinations = new List<string>
                    {
                        recipientAddress
                    },
                            RawMessage = new RawMessage
                            {
                                Data = stream
                            }
                        };

                        try
                        {
                            var response = await ses.SendRawEmailAsync(
                                request, cts.Token);
                            return response.MessageId;

                        }
                        catch (Amazon.SimpleEmail.AmazonSimpleEmailServiceException ex)
                        {
                            Scanner.Logger.Exception(ex, "SES service error: ");
                            Console.WriteLine(ex);

                            // Check if the underlying issue is the verification failure
                            string errorMessage = "The email service is temporarily unavailable.\n\n" +
                                                  "Please try again later or ask a library staff member for assistance.";

                            if (ex.Message.Contains("not verified") || ex.GetType().Name == "MessageRejectedException")
                            {
                                errorMessage = "The provided email address is unverified in this system.\n\n" +
                                               "Please use a verified address or contact an administrator.";
                            }

                            MessageBox.Show(
                                errorMessage,
                                "Email Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                        catch (Exception ex)
                        {
                            Scanner.Logger.Exception(ex, "Email sending failed: ");
                            Console.WriteLine(ex);
                            Console.WriteLine($"ERROR SENDING EMAIL\n{ex}");

                            MessageBox.Show(
                                "Your email could not be sent.\n\n" +
                                "Please try again later or ask a library staff member for assistance.",
                                "Email Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            Scanner.Logger.Exception(ex, "ERROR SENDING EMAIL");
                            
                        }

                        return null;
                    }
                }
            }
        }

    }
}