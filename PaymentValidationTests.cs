using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MMoney.Database.DbContexts;
using MMoney.Website.Library.Roster.Repayments;
using MMoney.Website.MobileMoney;
using MMoney.Database.Factories;
using System.Linq;

namespace MMoney.Test.UnitTests.MobileMoney.Kenya.Safaricom
{
    [TestClass]
    public class SafaricomPaymentValidationTests
    {
		private MobileMoneyValidationService mobileMoneyValidationService;
		private static RosterContext _readDb =DbContextFactory.GetRosterDbContext(DbInstanceType.Reporting, RosterInstance.Kenya);
        private static RosterContext _writeDb =DbContextFactory.GetRosterDbContext(DbInstanceType.Live, RosterInstance.Kenya);
		private int countryId = 404;

        #region request xml
        private string requestXml =
            @"<?xml>
                <soapenv:Envelope>
                    <soapenv:Body>
                        <ns1:C2BPaymentValidationRequest>...</ns1:C2BPaymentValidationRequest>
                    </soapenv:Body>
                </soapenv:Envelope>";
		#endregion

		public SafaricomPaymentValidationTests()
		{
			mobileMoneyValidationService = new MobileMoneyValidationService(_readDb, _writeDb);
			//turn validation on for kenya
			if (_readDb.DistrictSettings.Any(rs => rs.SettingName == "RepaymentValidationSetting" && rs.CountryId == countryId))
			{
				Database.Models.Roster.DistrictSetting repaymentValidationStatusSetting = _writeDb.DistrictSettings.SingleOrDefault(
						rs => rs.SettingName == "RepaymentValidationSetting" && rs.CountryId == countryId);
				repaymentValidationStatusSetting.SettingText = "Validate";
			}
			else
			{
				Database.Models.Roster.DistrictSetting repaymentValidationStatusSetting = new Database.Models.Roster.DistrictSetting();
				repaymentValidationStatusSetting.RegionalSettingId = _writeDb.DistrictSettings.Min(ds => ds.RegionalSettingId) > 0 ? -1 : _writeDb.DistrictSettings.Min(ds => ds.RegionalSettingId) - 1;
				repaymentValidationStatusSetting.SettingName = "RepaymentValidationSetting";
				repaymentValidationStatusSetting.SettingText = "Validate";
				repaymentValidationStatusSetting.CountryId = countryId;
				_writeDb.DistrictSettings.Add(repaymentValidationStatusSetting);
			}

		}

        //more unit ticke #6194
        /// <summary>
        /// Main phone number (exclusively main and not both main and mobile1/2) --> 250764242046
        /// </summary>
        [TestMethod]
        public void MobileMoney_InValid_MainPhoneNumber_Exclusive_Main()
        {
            GenericPayment testData = new GenericPayment();
            testData.Amount = 70;
            testData.DateOfTransaction = DateTime.Now;
            testData.MobilePhoneNumber = "254793814557";
            testData.PaybillBusinessShortCode = "555720";
            testData.TransactionStatus = "Completed";
            testData.TransactionCountryId = 404;
            testData.TransactionMethod = "Paybill (M-Pesa)";
            testData.TransactionType = "Payment";
            testData.TransactionCurrency = "KES";

            Random rn = new Random();
            testData.TransactionId = "Test" + rn.Next(100000, 999999).ToString();

            mobileMoneyValidationService.IsValidPayment(testData);

            Assert.IsTrue(testData.Response.code > 1);

        }
        /// <summary>
        /// Main phone number which is same as mobile 1 or 2 -> 254713796986
        /// </summary>
        [TestMethod]
        public void MobileMoney_InValid_MainPhoneNumber_SameMobile1or2()
        {
            GenericPayment testData = new GenericPayment();
            testData.Amount = 70;
            testData.DateOfTransaction = DateTime.Now;
            testData.MobilePhoneNumber = "254713796986";
            testData.PaybillBusinessShortCode = "555720";
            testData.TransactionStatus = "Completed";
            testData.TransactionCountryId = 404;
            testData.TransactionMethod = "Paybill (M-Pesa)";
            testData.TransactionType = "Payment";
            testData.TransactionCurrency = "KES";

            Random rn = new Random();
            testData.TransactionId = "Test" + rn.Next(100000, 999999).ToString();

            mobileMoneyValidationService.IsValidPayment(testData);

            Assert.IsTrue(testData.Response.code >1);
        }

        /// <summary>
        /// Dropped clients -> 10032233
        /// </summary>
        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
		public void MobileMoney_Valid_DroppedClients()
        {
            GenericPayment testData = new GenericPayment();
            testData.Amount = 70;
            testData.DateOfTransaction = DateTime.Now;
            testData.MobilePhoneNumber = "254777777777";
            testData.TransactionAccountNumber = "10032233";
            testData.PaybillBusinessShortCode = "555720";
            testData.TransactionStatus = "Completed";
            testData.TransactionCountryId = 404;
            testData.TransactionMethod = "Paybill (M-Pesa)";
            testData.TransactionType = "Payment";
            testData.TransactionCurrency = "KES";

            Random rn = new Random();
            testData.TransactionId = "Test" + rn.Next(100000, 999999).ToString();

            mobileMoneyValidationService.IsValidPayment(testData);

            Assert.AreEqual(1, testData.Response.code);
        }
        /// <summary>
        /// Inactive clients (Seasons don't matter) -> 11109558
        /// </summary>
        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
		public void MobileMoney_InValid_InactiveClient_IrrespectiveOfSeason()
        {
            GenericPayment testData = new GenericPayment();
            testData.Amount = 70;
            testData.DateOfTransaction = DateTime.Now;
            testData.MobilePhoneNumber = "254777777777";
            testData.TransactionAccountNumber = "11109558";
            testData.PaybillBusinessShortCode = "555720";
            testData.TransactionStatus = "Completed";
            testData.TransactionCountryId = 404;
            testData.TransactionMethod = "Paybill (M-Pesa)";
            testData.TransactionType = "Payment";
            testData.TransactionCurrency = "KES";

            Random rn = new Random();
            testData.TransactionId = "Test" + rn.Next(100000, 999999).ToString();

            mobileMoneyValidationService.IsValidPayment(testData);

            Assert.IsFalse(testData.Response.code> 1);
        }
        /// <summary>
        /// No Season Clients exist for a client -> 10171015 
        /// </summary>
        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
		public void MobileMoney_InValid_No_SeasonClient_for_Client()
        {
            GenericPayment testData = new GenericPayment();
            testData.Amount = 70;
            testData.DateOfTransaction = DateTime.Now;
            testData.MobilePhoneNumber = "254777777777";
            testData.TransactionAccountNumber = " 10171015";
            testData.PaybillBusinessShortCode = "555720";
            testData.TransactionStatus = "Completed";
            testData.TransactionCountryId = 404;
            testData.TransactionMethod = "Paybill (M-Pesa)";
            testData.TransactionType = "Payment";
            testData.TransactionCurrency = "KES";

            Random rn = new Random();
            testData.TransactionId = "Test" + rn.Next(100000, 999999).ToString();

            mobileMoneyValidationService.IsValidPayment(testData);

            Assert.IsFalse(testData.Response.code> 1);
        }
        /// <summary>
        /// Invalid account -> 9999999
        /// </summary>
        [TestMethod]
        public void MobileMoney_InValid_AccountNumber()
        {
            GenericPayment testData = new GenericPayment();
            testData.Amount = 70;
            testData.DateOfTransaction = DateTime.Now;
            testData.MobilePhoneNumber = "254777777777";
            testData.TransactionAccountNumber = "9999999";
            testData.PaybillBusinessShortCode = "555720";
            testData.TransactionStatus = "Completed";
            testData.TransactionCountryId = 404;
            testData.TransactionMethod = "Paybill (M-Pesa)";
            testData.TransactionType = "Payment";
            testData.TransactionCurrency = "KES";

            Random rn = new Random();
            testData.TransactionId = "Test" + rn.Next(100000, 999999).ToString();

            mobileMoneyValidationService.IsValidPayment(testData);

            Assert.IsTrue(testData.Response.code > 1);
        }
        /// <summary>
        /// Phone not existing -> 254777777777
        /// </summary>
        [TestMethod]
        public void MobileMoney_Invalid_PhoneNumber()
        {
            GenericPayment testData = new GenericPayment();
            testData.Amount = 70;
            testData.DateOfTransaction = DateTime.Now;
            testData.MobilePhoneNumber = "254777777777";
            testData.PaybillBusinessShortCode = "555720";
            testData.TransactionStatus = "Completed";
            testData.TransactionCountryId = 404;
            testData.TransactionMethod = "Paybill (M-Pesa)";
            testData.TransactionType = "Payment";
            testData.TransactionCurrency = "KES";

            Random rn = new Random();
            testData.TransactionId = "Test" + rn.Next(100000, 999999).ToString();

            mobileMoneyValidationService.IsValidPayment(testData);

            Assert.IsTrue(testData.Response.code > 1);
        }
        /// <summary>
        /// Duplicate and all duplicate are active -> 250723358500
        /// </summary>
        [TestMethod]
        public void MobileMoney_InValid_Duplicate_PhoeNumber_With_all_Duplicates_Active()
        {
            GenericPayment testData = new GenericPayment();
            testData.Amount = 70;
            testData.DateOfTransaction = DateTime.Now;
            testData.MobilePhoneNumber = "254723358500";
            testData.PaybillBusinessShortCode = "555720";
            testData.TransactionStatus = "Completed";
            testData.TransactionCountryId = 404;
            testData.TransactionMethod = "Paybill (M-Pesa)";
            testData.TransactionType = "Payment";
            testData.TransactionCurrency = "KES";

            Random rn = new Random();
            testData.TransactionId = "Test" + rn.Next(100000, 999999).ToString();

            mobileMoneyValidationService.IsValidPayment(testData);

            Assert.IsTrue(testData.Response.code > 1);
        }

    }
}
