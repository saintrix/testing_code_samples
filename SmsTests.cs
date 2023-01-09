using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MMoney.Database.DbContexts;
using MMoney.Database.Factories;
using MMoney.Database.Models.Roster;
using MMoney.Database.Models.Roster.Clients;
using MMoney.Database.Models.Roster.Sms;
using MMoney.Website.SMS;
using MMoney.Website.SMS.Infobip;

namespace MMoney.Test.UnitTests.SmsUnitTests
{
    [TestClass]
    public class SmsTests
    {
        private RosterContext _db;
        private MessagingService _messageService;
        private InfobipService _infobipService;

        public SmsTests()
        {
            _db = DbContextFactory.GetRosterDbContext(DbInstanceType.Live, RosterInstance.Kenya);
            _messageService = new MessagingService(_db, _db);
            _infobipService = new InfobipService();
        }

        #region Message Creation

        [TestMethod]
        public void Sms_General_Message_Token_Replacement()
        {
            const string textTemplate = 
                "Hello client {{name}}, you sent us {{amount}} of shillings and you currently have a balance of {{remaining}} left.";
            Dictionary<string, string> parameterList = new Dictionary<string, string>();
            string correctMessage = "";

            // add parameters to the dictionary
            parameterList.Add("name", "beatrice");
            parameterList.Add("amount", "8790.50");
            parameterList.Add("remaining", "1111000.50");

            correctMessage = "Hello client beatrice, you sent us 8790.50 of shillings and you currently have a balance of 1111000.50 left.";

            string finalizedMessage = _messageService.GenerateMessageText(textTemplate, parameterList);

            Assert.AreEqual(correctMessage, finalizedMessage);
        }

        #endregion

        #region Getting Correct API Key

        [TestMethod]
        public void Sms_General_Get_Correct_API_Key_By_Country_KE()
        {
            const int countryId = 404; 

            string apiKey = _infobipService.GetApiKeyByCountry(countryId, _db);
            string expectedApiKey = _db.CountrySetting(countryId, "InfobipCountryApiKey");

            Assert.AreEqual(apiKey, expectedApiKey);
        }

        [TestMethod]
        public void Sms_General_Get_Correct_API_Key_By_Country_RW()
        {
            const int countryId = 646; 

            string apiKey = _infobipService.GetApiKeyByCountry(countryId, _db);
            string expectedApiKey = _db.CountrySetting(countryId, "InfobipCountryApiKey");

            Assert.AreEqual(apiKey, expectedApiKey);
        }

        [TestMethod]
        public void Sms_General_Get_Correct_API_Key_By_Country_Invalid_CountryId()
        {
            const int countryId = 403; 

            string apiKey = _infobipService.GetApiKeyByCountry(countryId, _db);
            string expectedApiKey = "";

            Assert.AreEqual(apiKey, expectedApiKey);
        }

        #endregion

        #region Getting Correct Language

        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
		public void Sms_General_Get_Correct_Client_Language_In_District_With_Defined_Language()
        {
            const int districtId = 5404; // we explicity state that district 5404 should communicate in 'en'
            Client client = _db.Clients.Where(c => c.DistrictId == districtId).FirstOrDefault();

            string communicationLanguage = _messageService.getLanguage(client);
            string expectedCommunicationLanguage = _db.udf_GetRegionalSetting(districtId, "FieldLanguageID");

            Assert.AreEqual(communicationLanguage, expectedCommunicationLanguage);
        }

        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
		public void Sms_General_Get_Correct_Client_Language_In_District_With_No_Defined_Language()
        {
            const int districtId = 1404; // we explicity state that district 5404 should communicate in 'en'
            Client client = _db.Clients.Where(c => c.DistrictId == districtId).FirstOrDefault();

            string communicationLanguage = _messageService.getLanguage(client);
            string expectedCommunicationLanguage = _db.udf_GetRegionalSetting(districtId, "FieldLanguageID");

            Assert.AreEqual(communicationLanguage, expectedCommunicationLanguage);
        }

        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
        public void Sms_General_Get_Correct_Client_Language_In_Country_With_No_Defined_Language()
        {
            const int countryId = 800; // we explicity state that district 5404 should communicate in 'en'
            Client client = _db.Clients.Where(c => c.District.Region.CountryId == countryId).FirstOrDefault();

            string communicationLanguage = _messageService.getLanguage(client);
            string expectedCommunicationLanguage = "en";

            Assert.AreEqual(communicationLanguage, expectedCommunicationLanguage);
        }

        #endregion

        #region Getting Correct Translated Message Given a Language

        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
		public void Sms_General_Get_Correct_Translation_With_Swahili()
        {
            const int districtId = 1404;
            const string languageId = "sw";

            Client client = _db.Clients.Where(c => c.GlobalClientId.ToString() == "BBF80B05-1C4E-4B9B-89B2-000A77FEE733").FirstOrDefault();
            // now we need to get a message template
            MessageTextTemplate messageTextTemplate = _db.MessageTextTemplates
                .Where(mtt => mtt.MessageTextTemplateId == 1).FirstOrDefault();

            string expectedTranslation = _db.Translations
                .Where(t => t.LanguageId == languageId && t.TranslationKey == "messageTextTemplate-" + messageTextTemplate.MessageTextTemplateId + ".").FirstOrDefault()
                .Phrase;
            string translatePhrase = _messageService.getTranslation(client, messageTextTemplate);

            Assert.AreEqual(translatePhrase, expectedTranslation);
        }

        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
		public void Sms_General_Get_Correct_Translation_With_English()
        {
            const int districtId = 5404;
            const string languageId = "en";
			Guid G = Guid.Parse("35EA64B3-56F0-4AED-8ABD-0002FB568063");
			Client client = _db.Clients.Where(c => c.GlobalClientId.Equals(G)).FirstOrDefault();
			// now we need to get a message template
			MessageTextTemplate messageTextTemplate = _db.MessageTextTemplates
                .Where(mtt => mtt.MessageTextTemplateId == 1).FirstOrDefault();

            string expectedTranslation = _db.Translations
                .Where(t => t.LanguageId == languageId && t.TranslationKey == "messageTextTemplate-" + messageTextTemplate.MessageTextTemplateId + ".").FirstOrDefault()
                .Phrase;
            string translatePhrase = _messageService.getTranslation(client, messageTextTemplate);

            Assert.AreEqual(translatePhrase, expectedTranslation);
        }

        #endregion

        #region Assemble Correct Message For Client

        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
		public void Sms_General_Create_Correct_Message_Swahili()
        {
            const int districtId = 1404;
            Client client = _db.Clients.Where(c => c.GlobalClientId.ToString() == "BBF80B05-1C4E-4B9B-89B2-000A77FEE733").FirstOrDefault();
            MessageTextTemplate messageTextTemplate = _db.MessageTextTemplates
                .Where(mtt => mtt.MessageTextTemplateId == 1).FirstOrDefault();
            Dictionary<string, string> parameterList = new Dictionary<string, string>();

            parameterList.Add("amount", "12345");
            parameterList.Add("firstName", "beatrice");
            parameterList.Add("receiptId", "ZXCYUIL98&%$2");
            parameterList.Add("amountTotalPaid", "123456");
            parameterList.Add("amountRemaining", "1234567");

            Sms sms = _messageService.GenerateSmsMessage(client, messageTextTemplate, parameterList);

            string expectedMessage =
                "Jambo beatrice. Malipo ya mwisho: KSh 12345. Nambari ya risiti ZXCYUIL98&%$2. Malipo kwa ujumla KSh 123456. Malipo yaliyobaki KSh 1234567.";

            Assert.AreEqual(sms.text, expectedMessage);
        }

        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
		public void Sms_General_Create_Correct_Message_English()
        {
            const int districtId = 5404;
			Guid guid = Guid.Parse("35EA64B3-56F0-4AED-8ABD-0002FB568063");
			Client client = _db.Clients.Where(c => c.GlobalClientId.Equals(guid)).FirstOrDefault();
			MessageTextTemplate messageTextTemplate = _db.MessageTextTemplates
                .Where(mtt => mtt.MessageTextTemplateId == 1).FirstOrDefault();
            Dictionary<string, string> parameterList = new Dictionary<string, string>();

            parameterList.Add("amount", "12345");
            parameterList.Add("firstName", "beatrice");
            parameterList.Add("receiptId", "ZXCYUIL98&%$2");
            parameterList.Add("amountTotalPaid", "123456");
            parameterList.Add("amountRemaining", "1234567");

            Sms sms = _messageService.GenerateSmsMessage(client, messageTextTemplate, parameterList);

            string expectedMessage =
                "Hello beatrice. Last payment: KSh 12345. Receipt number ZXCYUIL98&%$2. Total paid KSh 123456. Balance KSh 1234567.";

            Assert.AreEqual(sms.text, expectedMessage);
        }

        #endregion

        #region Bulk Assemble Messages for Many Clients

        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
		public void Sms_General_Create_Many_Correct_Messages()
        {
            List<Client> clients = new List<Client>();
            List<Dictionary<string, string>> parametersLists = new List<Dictionary<string, string>>();
            MessageTextTemplate messageTextTemplate = _db.MessageTextTemplates
                .Where(mtt => mtt.MessageTextTemplateId == 1).FirstOrDefault();

            // lets add a client in a swahili speaking district
            clients.Add( 
                _db.Clients.Where( c => c.DistrictId == 1404).FirstOrDefault()
            );

            Dictionary<string, string> parameterListSwahili = new Dictionary<string, string>();
            parameterListSwahili.Add("firstName", "beatrice");
            parameterListSwahili.Add("amount", "12345");
            parameterListSwahili.Add("receiptId", "ZXCYUIL98&%$2");
            parameterListSwahili.Add("amountTotalPaid", "123456");
            parameterListSwahili.Add("amountRemaining", "1234567");
            parametersLists.Add(parameterListSwahili);
            string expectedMessageSwahili =
                "Jambo beatrice. Malipo ya mwisho: KSh 12345. Nambari ya risiti ZXCYUIL98&%$2. Malipo kwa ujumla KSh 123456. Malipo yaliyobaki KSh 1234567.";

            // now let's add a client in an english speaking district
            clients.Add( 
                _db.Clients.Where( c => c.DistrictId == 5404).FirstOrDefault()
            );

            Dictionary<string, string> parameterListEnglish = new Dictionary<string, string>();
            parameterListEnglish.Add("firstName", "barnabas");
            parameterListEnglish.Add("amount", "54321");
            parameterListEnglish.Add("receiptId", "AbCdjUI90");
            parameterListEnglish.Add("amountTotalPaid", "654321");
            parameterListEnglish.Add("amountRemaining", "7654321");
            parametersLists.Add(parameterListEnglish);
            string expectedMessageEnglish =
                "Hello barnabas. Last payment: KSh 54321. Receipt number AbCdjUI90. Total paid KSh 654321. Balance KSh 7654321.";

            // now let's generate the messages
            List<Sms> messages = _messageService.GenerateSmsMessages(clients, messageTextTemplate, parametersLists);

            // and let's compare them
            Assert.AreEqual(expectedMessageSwahili, messages[0].text);
            Assert.AreEqual(expectedMessageEnglish, messages[1].text);
        }

        #endregion

        #region Create an Infobip Message from a Generic Message

        [TestMethod]
        public void Sms_Infobip_Create_InfoBip_Message_From_Generic_Message()
        {
            string text = "Hello beatrice, thanks for your payment.";
            string to = "254706822219";
            string from = "65778";

            Sms testGenericMessage = new Sms(text, from, to);
            InfobipSms testInfobipMessage = new InfobipSms(testGenericMessage);

            Assert.AreEqual(testGenericMessage.text, testInfobipMessage.text);
            Assert.AreEqual(testGenericMessage.phoneNumber_from, testInfobipMessage.from);
            Assert.AreEqual(testGenericMessage.phoneNumber_to, testInfobipMessage.to[0]);
        }

        #endregion
    }
}
