using System;
using Twilio;
using Twilio.Types;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using Speech_Recognition.Data.Functions;

namespace Speech_Recognition.Data.Applications
{
    public class WhatsAppData
    {
        // WhatsApp default vars.
        private KeyValuePair<String, String> iMyNumber;
        private String iTwilioSID, iTwilioAuthToken, iMessageServiceSID;

        // WhatsAppData constructor
        public WhatsAppData(KeyValuePair<String, String> _myNumber, String _TwilioSID, String _TwilioAuthToken, String _MessageServiceSID) {
            this.iMyNumber = _myNumber;
            this.iTwilioSID = _TwilioSID;
            this.iTwilioAuthToken = _TwilioAuthToken;
            this.iMessageServiceSID = _MessageServiceSID;
        }

        // Trigger a rest TwilioClient initialization
        public void connectTwilio() { TwilioClient.Init(iTwilioSID, iTwilioAuthToken); }

        // Complete the connection of the rest TwilioClient
        public void disconnectTwilio() { TwilioClient.Invalidate(); }

        // Get connection status of the rest TwilioClient
        public KeyValuePair<bool, String> getStatusTwilioConn() {
            if (String.IsNullOrWhiteSpace(iTwilioSID) || String.IsNullOrWhiteSpace(iTwilioAuthToken))
                return new KeyValuePair<bool, String>(false, String.Empty);
            else {
                try {
                    Twilio.Clients.ITwilioRestClient restClient = TwilioClient.GetRestClient();
                    return String.IsNullOrWhiteSpace(restClient.AccountSid) ? 
                        new KeyValuePair<bool, String>(false, SpeechChoices.WhatsAppData.botComms[2]) : 
                        new KeyValuePair<bool, String>(true, SpeechChoices.WhatsAppData.botComms[0]);
                } catch (AuthenticationException excep) {
                    return new KeyValuePair<bool, String>(false, excep.Message);
                }
            }
        }

        // Check if everything is ready to send the message
        public KeyValuePair<bool, String> whatsAppReadyToAction(bool _dataBool, String _messageTo) {
            if (_dataBool) {
                if (getStatusTwilioConn().Key) {
                    if (_messageTo.Equals(iMyNumber.Key)) return new KeyValuePair<bool, String>(false, SpeechChoices.WhatsAppData.botComms[6]);
                    else return new KeyValuePair<bool, String>(true, String.Empty);
                } else return new KeyValuePair<bool, String>(false, SpeechChoices.WhatsAppData.botComms[2]);
            } else return new KeyValuePair<bool, String>(false, $"Must enable { GrammarData.strBotData.Keys.ToList()[5] } data first");
        }

        // Send an SMS message through TwilioClient
        public String sendSMSMessage(bool _dataBool, String _fullSpeech, String _messageTo) {
            KeyValuePair<bool, String> readyToSend = whatsAppReadyToAction(_dataBool, _messageTo);
            if (readyToSend.Key) {
                String strParse = _fullSpeech.Remove(0, (_fullSpeech.IndexOf(_messageTo) + _messageTo.Length) + 6);
                MessageResource.StatusEnum msgResult = sendSMSMssgTo(GrammarData.cContactsList[_messageTo], strParse);
                return $"The SMS message is { msgResult }";
            } else return readyToSend.Value;
        }

        // Send an SMS message to a defined contact with a given text message
        private MessageResource.StatusEnum sendSMSMssgTo(String _contact, String _text) {
            try {
                var messageOptions = new CreateMessageOptions(new PhoneNumber(_contact));
                messageOptions.MessagingServiceSid = iMessageServiceSID;
                messageOptions.Body = _text;
                var message = MessageResource.Create(messageOptions);
                Thread.Sleep(2000);
                return message.Status; // Return StatusEnum options
            } catch (AuthenticationException) {
                return new MessageResource.StatusEnum();
            }
        }

        // Send a WhatsApp message through TwilioClient
        public String sendWhatsAppMessage(bool _dataBool, String _fullSpeech, String _messageTo) {
            KeyValuePair<bool, String> readyToSend = whatsAppReadyToAction(_dataBool, _messageTo);
            if (readyToSend.Key) {
                String strParse = _fullSpeech.Remove(0, (_fullSpeech.IndexOf(_messageTo) + _messageTo.Length) + 6);
                MessageResource.StatusEnum msgResult = sendWhatsAppMssgTo(iMyNumber.Value, GrammarData.cContactsList[_messageTo], strParse);
                return $"The WhatsApp message is { msgResult }";
            } else return readyToSend.Value;
        }

        // Send a WhatsApp message to a defined contact with a given text message
        private MessageResource.StatusEnum sendWhatsAppMssgTo(String _from, String _to, String _text) {
            try {
                var messageOptions = new CreateMessageOptions(new PhoneNumber($"whatsapp:{ _to }"));
                messageOptions.From = new PhoneNumber($"whatsapp:{ _from }");
                messageOptions.Body = _text;
                var message = MessageResource.Create(messageOptions);
                Thread.Sleep(2000);
                return message.Status; // Return StatusEnum options
            } catch (AuthenticationException) {
                return new MessageResource.StatusEnum();
            }
        }

        // Send a WhatsApp message to a defined contact with a given list of images
        public List<MessageResource.StatusEnum> sendWhatsAppImgsTo(String _ImgbbAPI, String _to) {
            try {
                List<MessageResource.StatusEnum> listMsgStatus = new List<MessageResource.StatusEnum>();
                // Trigger a filedialog to choose images location
                OpenFileDialog localImages = new OpenFileDialog();
                localImages.Title = "WhatsApp Images";
                localImages.InitialDirectory = @"C:\";
                localImages.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";
                localImages.FilterIndex = 2;
                localImages.RestoreDirectory = true;
                localImages.Multiselect = true;
                if (localImages.ShowDialog() == DialogResult.OK) {
                    foreach (String file in localImages.FileNames) {
                        String tmpFileURL = APIsFunctions.uploadImgbbFile(_ImgbbAPI, new Uri(file).OriginalString);
                        // Message data and content build
                        var messageOptions = new CreateMessageOptions(new PhoneNumber($"whatsapp:{ _to }"));
                        messageOptions.From = new PhoneNumber($"whatsapp:{ iMyNumber.Value }");
                        messageOptions.Body = String.Empty;
                        messageOptions.MediaUrl = new List<Uri>() { new Uri(tmpFileURL) };
                        var message = MessageResource.Create(messageOptions);
                        Thread.Sleep(2000);
                        listMsgStatus.Add(message.Status);
                    }
                }
                return listMsgStatus;
            } catch (AuthenticationException) {
                return new List<MessageResource.StatusEnum>();
            } catch (ApiException) {
                return new List<MessageResource.StatusEnum>();
            }
        }
    }
}