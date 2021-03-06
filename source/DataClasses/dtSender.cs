﻿using CheccoSafetyTools;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Outlook = Microsoft.Office.Interop.Outlook;
using cst_WHOIS = CheccoSafetyTools.cst_WHOISNET_API;

namespace OutlookSafetyChecks
{
    public class dtSender : dtTemplate
    {
        public dtSender()
        {
			this.Columns.Add("Field", Type.GetType("System.String"));
			this.Columns.Add("Name", Type.GetType("System.String"));
			this.Columns.Add("Address", Type.GetType("System.String"));
			this.Columns.Add("Owner", Type.GetType("System.String"));
			this.Columns.Add("Notes", Type.GetType("System.String"));
		}

		public override int buildData(dsMailItem parent, Outlook.MailItem myItem)
        {
			String logTitle = Properties.Resources.Title_Contacts + " / Sender";
			// Obtain "From:"
			String senderName = myItem.SenderName;
			String senderEmail = myItem.SenderEmailAddress;
			String senderOwner = "[not checked]";
			String senderNotes = "";
			String senderHost = null;
			String senderDomain = null;
			String senderUser = null;
			try
			{
                senderEmail = senderEmail.Replace("<", "").Replace(">", "");
                MailAddress senderAddress = new MailAddress(senderEmail,senderName);
				// grab domain owner for email domain   
				senderUser = senderAddress.User;
				senderHost = senderAddress.Host;
				senderDomain = cst_Util.pullDomain(senderHost);
				// check email
				if (Properties.Settings.Default.opt_Lookup_WHOIS)
				{
					senderOwner = cst_WHOIS.whoisOwner(senderDomain, Properties.Settings.Default.opt_Use_CACHE);
				}
				senderNotes = checkEmail(senderAddress, logTitle);
			}
			catch (Exception ex)
            {
				senderNotes += "[* Invalid \"From:\" Email Address Specified]";
				parent.log(logTitle, "1", "INVALID DATA", "Invalid [From:] Email Address Specified");
                cst_Util.logException(ex, "Parsing From: " + senderEmail);
            }
            // add row
            String[] rowData = new[] { "From", senderName, senderEmail, senderOwner, senderNotes };
			this.Rows.Add(rowData);

			// Obtain "ReplyTo:"
			foreach (Outlook.Recipient tReplyAddr in myItem.ReplyRecipients)
			{
				String tTag = cst_Outlook.getRecipientTag(tReplyAddr);
				String tType = cst_Outlook.getRecipientType(tReplyAddr);
				// Obtain Sender (Reply-To:)
				String replyToName = tReplyAddr.Name;
				String replyToEmail = tReplyAddr.Address;
				String replyToOwner = "[not checked]";
				String replyToNotes = "";
				// grab domain owner for email domain 
				try
				{
                    replyToEmail = replyToEmail.Replace("<", "").Replace(">", "");
                    MailAddress replyToAddress = new MailAddress(replyToEmail,replyToName);
					String replyToHost = replyToAddress.Host;
					String replyToDomain = cst_Util.pullDomain(replyToHost);
					// start checks
					if (Properties.Settings.Default.opt_Lookup_WHOIS)
					{
						replyToOwner = cst_WHOIS.whoisOwner(replyToDomain, Properties.Settings.Default.opt_Use_CACHE);
					}
					replyToNotes = checkEmail(replyToAddress,logTitle);
					// advanced checks
					if (replyToEmail != senderEmail)
					{
						replyToNotes += "[* MISMATCHED From/ReplyTo]: ";
						String noteDetails = "";
						if (replyToAddress.User != senderUser)
						{
							noteDetails += "USER, ";
						}
						if (replyToDomain != senderDomain)
						{
							noteDetails += "DOMAIN, ";
						}
						else if (replyToHost != senderHost)
						{
							noteDetails += "SERVER, ";
						}
						replyToNotes += noteDetails + "\r\n";
						parent.log(logTitle, "1", "MISMATCHED From/"+tTag, noteDetails);
					}
				}
				catch (Exception ex)
                {
					replyToNotes += "[* Invalid \"" + tTag + ":\" Email Address Specified]";
					parent.log(logTitle, "1", "INVALID DATA", "Invalid [" + tTag + ":] Email Address Specified");
                    cst_Util.logException(ex, "Parsing " + tTag + ": " + replyToEmail);
                }
                rowData = new[] { tTag, replyToName, replyToEmail, replyToOwner, replyToNotes };
				this.Rows.Add(rowData);
			}

			// Obtain "Return-Path:"
			List<String> arrReply = new List<string>();
			dtHeaders tHeaders = parent.findTableClass<dtHeaders>() as dtHeaders;
			if (tHeaders != null)
			{
				if (tHeaders.Rows.Count == 0) tHeaders.populate(false);
				foreach (DataRow tRow in tHeaders.Rows)
				{
					String tKey = tRow.ItemArray[0] as String;
					String tVal = tRow.ItemArray[1] as String;
					if (tKey == "Return-Path") arrReply.Add(tVal);
				}
			}
            foreach (String replyToEmail in arrReply)
            {
                // Obtain Sender (Return-Path:)
                String replyToOwner = "[not checked]";
                String replyToNotes = "";
                // grab domain owner for email domain            
                try
                {
                    String tmpEmail = replyToEmail.Replace("<", "").Replace(">", "");
                    MailAddress replyToAddress = new MailAddress(tmpEmail);
                    String replyToHost = replyToAddress.Host;
                    String replyToDomain = cst_Util.pullDomain(replyToHost);
                    // start checks
                    if (Properties.Settings.Default.opt_Lookup_WHOIS)
                    {
                        replyToOwner = cst_WHOIS.whoisOwner(replyToDomain, Properties.Settings.Default.opt_Use_CACHE);
                    }
                    replyToNotes = checkEmail(replyToAddress, logTitle);
                    // advanced checks
                    if (replyToEmail != senderEmail)
                    {
                        replyToNotes += "[* MISMATCHED From/Return-Path]: ";
                        String noteDetails = "";
                        if (replyToAddress.User != senderUser)
                        {
                            noteDetails += "USER, ";
                        }
                        if (replyToDomain != senderDomain)
                        {
                            noteDetails += "DOMAIN, ";
                        }
                        else if (replyToHost != senderHost)
                        {
                            noteDetails += "SERVER, ";
                        }
                        replyToNotes += noteDetails + "\r\n";
                        parent.log(logTitle, "1", "MISMATCHED From/Return-Path", noteDetails);
                    }
                }
                catch (Exception ex)
                {
                    replyToNotes += "[* Invalid \"Return-Path:\" Email Address Specified]";
                    parent.log(logTitle, "1", "INVALID DATA", "Invalid [Return-Path:] Email Address Specified");
                    cst_Util.logException(ex, "Parsing Return-Path: " + replyToEmail);
                }
                rowData = new[] { "Return-Path", "", replyToEmail, replyToOwner, replyToNotes };
                this.Rows.Add(rowData);
            }
			return this.Rows.Count;
        }
    } // class
} // namespace
