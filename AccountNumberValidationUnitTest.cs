using Microsoft.VisualStudio.TestTools.UnitTesting;
using MMoney.Database.DbContexts;
using MMoney.Database.Factories;
using MMoney.Website.MobileMoney;
using MMoney.Website.MobileMoney.Lipisha;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMoney.Test.UnitTests.MobileMoney.Lipisha
{
    [TestClass]
    public class AccountNumberValidationUnitTest
    {
        MobileMoneyValidationService mobileMoneyValidator;
        GenericPayment testData;
        LipishaData lipishaData;
		int countryId = 404;

		public AccountNumberValidationUnitTest()
		{
			RosterContext _readDb = DbContextFactory.GetRosterDbContext(DbInstanceType.Reporting, RosterInstance.Kenya);
			RosterContext _writeDb = DbContextFactory.GetRosterDbContext(DbInstanceType.Live, RosterInstance.Kenya);

			mobileMoneyValidator = new MobileMoneyValidationService(_readDb, _writeDb);

			//turn validation on for kenya
			if (_readDb.DistrictSettings.Any(rs => rs.SettingName == "RepaymentValidationSetting" && rs.CountryId == countryId))
			{
				Database.Models.CRM.DistrictSetting repaymentValidationStatusSetting = _writeDb.DistrictSettings.SingleOrDefault(
						rs => rs.SettingName == "RepaymentValidationSetting" && rs.CountryId == countryId);
				repaymentValidationStatusSetting.SettingText = "Validate";
			}
			else
			{
				Database.Models.CRM.DistrictSetting repaymentValidationStatusSetting = new Database.Models.CRM.DistrictSetting();
				repaymentValidationStatusSetting.RegionalSettingId = _writeDb.DistrictSettings.Min(ds => ds.RegionalSettingId) > 0 ? -1 : _writeDb.DistrictSettings.Min(ds => ds.RegionalSettingId) - 1;
				repaymentValidationStatusSetting.SettingName = "RepaymentValidationSetting";
				repaymentValidationStatusSetting.SettingText = "Validate";
				repaymentValidationStatusSetting.CountryId = countryId;
				_writeDb.DistrictSettings.Add(repaymentValidationStatusSetting);
			}

			//valid input, client Id = 1221, DistrictId = 6646
			lipishaData = new LipishaData(
				api_key: "132a66e2b76e06",
				api_signature: "NdDwcaeXcYDgk2BVkEWDUn+o8H3ViDfL/i0Tr04BN+UhX5CMqYTFCTCNY8iK3jsu8Q9Irr4IGL6lGFIVcOFOAlMNznSvovQ+oYBadZ34g9NMFqyE5qOkLt/CNZpZzABSmjdk=",
				api_type: "Initiate",
				transaction_reference: "234234",
				transaction_status: "Completed",
				transaction_status_code: "CU79AW109D",
				transaction_status_description: null,
				transaction_account: "13559830",
				transaction_mobile: "2**710821667",
				transaction_status_action: null,
				transaction_status_reason: null,
				transaction_country: "KE",
				transaction_amount: "1", //todo find out if there is a minimum/maximum amount that can be paid per country instance
				transaction_paybill: "961700", //todo confirm correct paybill for different providers
				transaction_method: "Paybill (M-Pesa)",
				transaction_type: "Payment",
				transaction_currency: "KES",
				transaction_date: DateTime.Now.ToShortTimeString(),
				transaction_merchant_reference: "1",
				transaction_code: "CU7W109D"
			   );

			testData = new GenericPayment(lipishaData);

		}

        //for each test case the Transaction Reference has to be randomized to prevent duplicate payment exception
        [TestMethod]
        public void Lipisha_BlankAccountNumberTest() //1 - payment should be rejected
        {
            lipishaData.transaction_code = "CU7J109Q";
            lipishaData.transaction_reference = "234234";
            lipishaData.transaction_account = "";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);
            Assert.IsTrue(testData.Response.code > 1);  
            // TODO: make sure that we check that we've identified that we're looking for an invalid account number error
            //FAILED : the payment was not rejected because the phone number was identifed as valid
        }

        [TestMethod]
        public void Lipisha_SpacesAndSpecialCharactersOnlyInAccountNumberTest() //1a - payment should be rejected
        {
            lipishaData.transaction_code = "CUAW1FES";
            lipishaData.transaction_account = "~`!@#$%^&*()_-+=}]{[|:;'?/>.<,";
            //NB cannot exahust all characters without regex, the full range extends to the ASCI/Unicode character set

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);
            Assert.IsTrue(testData.Response.code > 0);
            // TODO make sure that we're comparing the error code against an invalid account numbver
            ////FAILED : the payment was not rejected because the phone number was identifed as valid
        }

        [TestMethod]
		public void Lipisha_SingleCharacterAccountNumberTestWithValidPhoneNumber() //2 
        {
            lipishaData.transaction_code = "CU79GAD09Q";
            lipishaData.transaction_mobile = "2**786614795";
            lipishaData.transaction_account = "j";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);
			//Invalid account should fail
            Assert.IsTrue(testData.Response.code > 1);
        }

        [TestMethod]
        public void Lipisha_SingleCharacterAccountNumberTestWithInvalidPhoneNumber() //2 - phone number validation to be done
        {
            lipishaData.transaction_code = "CU7W16OG2";
            lipishaData.transaction_mobile = "2**711000000";
            lipishaData.transaction_account = "j";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);

            Assert.IsTrue(testData.Response.code > 1);
            //todo identify the correct response code to assert against
        }

        [TestMethod]
        public void Lipisha_AlphanumericAccountNumberTestValidPhoneNumber()//2a Phone Number Validation to be done
        {
            lipishaData.transaction_code = "CU924FA4G";
            lipishaData.transaction_mobile = "2**710821667";
            lipishaData.transaction_account = "1a";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);
            Assert.IsTrue(testData.Response.code > 1); //Failed, returned invalid transaction
            // TODO: make sure we are checking against an invalid transaction code
        }


        [TestMethod]
        public void Lipisha_AlphanumericAccountNumberTestInvalidPhoneNumber()//2a Phone Number validation to be done
        {
            lipishaData.transaction_code = "CUM94QW109Q";
            lipishaData.transaction_account = "1a";
            lipishaData.transaction_mobile = "2**711000000";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);
            Assert.IsTrue(testData.Response.code > 1);
            //Passed
            //todo find out if we need to check on the actual invalid reponse code e.g. invalid account number, invalid phone number etc
        }

        [TestMethod]
        public void Lipisha_AllDigitAccountNumberTestWithoutValidClient()//3 & 3a Validate against account number
        {
            lipishaData.transaction_code = "CU797MN98GJA49FA";
            lipishaData.transaction_account = "11111111"; //invalid account number
            lipishaData.transaction_mobile = "2**710821667";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);

            // Assert.IsTrue(response != LipishaValidationService.VALID_TRANSACTION);
            Assert.IsTrue(testData.Response.code > 1);
            // TODO: check to make sure we are validating agains an invalid transaction
        }

        [TestMethod]
        public void Lipisha_ShortAccountNumber()//4 & 4a Validate against account number
        {
            lipishaData.transaction_code = "CU79A2ZC4BA46GA";
            lipishaData.transaction_account = "135";
            lipishaData.transaction_mobile = "2**710821667";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);

            Assert.IsTrue(testData.Response.code > 1);
        }

        [TestMethod]
		[TestCategory("FailsWithMinimizedDb")]
        public void Lipisha_EightDigitsPlusExtraSpecialCharactersTest()//5 & 5a Validate against account number
        {
            lipishaData.transaction_code = "CU79IMMoneyAKI443JGJ";
            lipishaData.transaction_account = "1,3,5,&5!9:8?3&0*";
            lipishaData.transaction_mobile = "2**710821667";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);

            //special characters should be trimmed down to 13559830 which is a valid account number
            Assert.AreEqual(1, testData.Response.code);
            //FAILED
        }

        [TestMethod]
        public void Lipisha_EightDigitsPlusExtraSpecialCharactersAndNoMatchingClientTest()//5b Validate against account number
        {
            lipishaData.transaction_code = "CVU7345GA43FDA6";
            lipishaData.transaction_account = "1,1<1{1:1)1*1@1^!";
            lipishaData.transaction_mobile = "2**710821667";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);
            //trimmed account number should not match existing client
            Assert.IsTrue(testData.Response.code > 1);
        }

        [TestMethod]
		public void Lipisha_CombinedAlphaNumberAndSpecialCharacterTest_ValidPhoneNumber()//7a validate against phone number
        {
            lipishaData.transaction_code = "CU73484KFA3FAA6";
            lipishaData.transaction_account = "K,B,A";
            lipishaData.transaction_mobile = "2**786614795";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);

            //invalid account number should fail
            Assert.IsTrue(testData.Response.code > 1);
        }

        [TestMethod]
        public void Lipisha_CombinedAlphaNumberAndSpecialCharacterTest_InvalidPhoneNumber()//7 validate against phone number
        {
            lipishaData.transaction_code = "CUN3874KFA3FA6";
            lipishaData.transaction_account = "K,B,A";
            lipishaData.transaction_mobile = "2**70111111";

            testData = new GenericPayment(lipishaData);

            mobileMoneyValidator.IsValidPayment(testData);
            //trimmed account number should not match existing client
            Assert.IsTrue(testData.Response.code > 1);
            //TODO: compare against an invalid transaction code

        }
    }
}
