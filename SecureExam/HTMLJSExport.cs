﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Cryptography;

namespace SecureExam
{
    class HTMLJSExport : IQuestionsExport
    {
        public bool export(String filename)
        {
            StreamReader htmlSkeleton = new StreamReader(BasicSettings.getInstance().exportSkeletons[OutputType.HTMLJS]);
            StreamWriter outFile = new StreamWriter(filename);

            if (BasicSettings.getInstance().Encryption.AES.questionsAESKey == null)
            {
                BasicSettings.getInstance().Encryption.AES.questionsAESKey = Helper.getSecureRandomBytes(BasicSettings.getInstance().Encryption.AES.KeyLength/8);
                BasicSettings.getInstance().Encryption.AES.questionsAESKeyIV = Helper.getSecureRandomBytes(BasicSettings.getInstance().Encryption.AES.IvLength/8);
            }

            try
            {
                // read skeleton
                String html = htmlSkeleton.ReadToEnd();

                // Replace the placeholders in HTML code with real data
                html = html.Replace("$ENCRYPTEDDATA$", this.exportQuestions());
                html = html.Replace("$USERKEYDB$", this.exportUserKeyDB());
                html = html.Replace("$SUBJECT$", DataProvider.getInstance().examDetails.subject);
                html = html.Replace("$EXAMTITLE$", DataProvider.getInstance().examDetails.examTitle);
                html = html.Replace("$EXAMNOTES$", DataProvider.getInstance().examDetails.examNotes);

                // set variables & constants
                html = html.Replace("$SHA256ITERATIONS$", BasicSettings.getInstance().Encryption.SHA256.Iterations.ToString());
                html = html.Replace("$HISTORYTIMEMAXVARIANCE$", "5000");
                html = html.Replace("$INTERNALTIMEMAXVARIANCE$", "5000");
                html = html.Replace("$CONFIRMAUTOSAVERESTORE$", "true");
                html = html.Replace("$EBOOKREADEREXPORT$", "false");

                // Activate choosen security features
                StringBuilder listeners = new StringBuilder();
                if (true) // internetAccess
                {
                    listeners.Append("exam.addEventListener(SecureExam.Event.InternetAccess.ONLINE, isOnline);\n");
                }
                if (true) // tabchange
                {
                    listeners.Append("exam.addEventListener(SecureExam.Event.SecureTime.TABCHANGE, tabChange);\n");
                }
                html = html.Replace("$LISTENERS$", listeners.ToString());

                // write data to file
                outFile.Write(html);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            finally
            {
                htmlSkeleton.Close();
                outFile.Close();
            }
            return true;
        }

        private string exportQuestions()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Helper.ByteArrayToHexString(BasicSettings.getInstance().Encryption.AES.questionsAESKeyIV));
            sb.Append(",");

            StringBuilder dataToEncrypt = new StringBuilder();
            dataToEncrypt.Append(Helper.dateTimeToMillisecondsSince1970ForJS( DataProvider.getInstance().examDetails.examStartTime ) );
            dataToEncrypt.Append(",");
            dataToEncrypt.Append(Helper.dateTimeToMillisecondsSince1970ForJS( DataProvider.getInstance().examDetails.examEndTime ) );
            dataToEncrypt.Append(",");
            dataToEncrypt.Append(DataProvider.getInstance().examDetails.examDurationMinutes );
            dataToEncrypt.Append(",");
            dataToEncrypt.Append(this.generateQuestionsHTML());

            sb.Append(Helper.ByteArrayToHexString(Helper.encryptAES(dataToEncrypt.ToString(), BasicSettings.getInstance().Encryption.AES.questionsAESKey, BasicSettings.getInstance().Encryption.AES.questionsAESKeyIV)));
            return sb.ToString();
        }

        private string exportUserKeyDB()
        {
            if (BasicSettings.getInstance().Encryption.AES.questionsAESKey != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Participant participant in DataProvider.getInstance().Participants)
                {
                    byte[] salt = Helper.getSecureRandomBytes(BasicSettings.getInstance().Encryption.SHA256.SaltLength/8);
                    byte[] aesIV = Helper.getSecureRandomBytes(BasicSettings.getInstance().Encryption.AES.IvLength/8);
                    byte[] userHAsh = Helper.SHA256(participant.StudentSecret, salt, BasicSettings.getInstance().Encryption.SHA256.Iterations);
                    string encryptedMasterKey = Helper.ByteArrayToHexString( Helper.encryptAES(Helper.ByteArrayToHexString(BasicSettings.getInstance().Encryption.AES.questionsAESKey), userHAsh, aesIV) );

                    if( participant.GetType() == typeof(Student))
                    {
                        sb.Append(((Student)participant).studentPreName);
                        sb.Append(((Student)participant).studentSurName);
                        sb.Append(((Student)participant).studentID);
                        Debug.WriteLine("STUDENT-> Vorname: " + ((Student)participant).studentPreName + " Nachname: " + ((Student)participant).studentSurName + " ID: " + ((Student)participant).studentID + " Passwort: " + ((Student)participant).secret);
                    }
                    else if( participant.GetType() == typeof(Professor))
                    {
                        sb.Append(((Professor)participant).name);
                        Debug.WriteLine("PROFESSOR-> Name: " + ((Professor)participant).name + " UserSecret: " + ((Professor)participant).secret);
                    }
                    sb.Append(",");
                    sb.Append(encryptedMasterKey);
                    sb.Append(",");
                    sb.Append(Helper.ByteArrayToHexString(aesIV));
                    sb.Append(",");
                    sb.Append(Convert.ToBase64String(salt));
                    sb.Append(";");

                }
                return sb.ToString();
            }
            else
                throw new NoAESKeyException("No Masterkey found");
        }

        private string generateQuestionsHTML()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<form id=\"exam\">");
            foreach (Question question in DataProvider.getInstance().Questions)
            {
                sb.AppendLine("<div class=\"question\">");
                sb.AppendLine("<p class=\"questionText\">" + question.text + "</p>");
                sb.AppendLine("<p class=\"answer\">");
                switch (question.questionType)
                {
                    case QuestionType.CHECK_BOX:
                        foreach (Answer answer in question.answers)
                        {
                            sb.AppendLine("<input type=\"checkbox\" class=\"checkbox\" />" + answer.text + "<br>");
                        }
                        break;
                    case QuestionType.TEXT_BOX:
                        sb.Append("<textarea rows=\"6\" class=\"textBox\" ");
                        if( question.answers[0].placeHolder != null )
                            sb.Append("placeholder=\"" + question.answers[0].placeHolder + "\"");
                        sb.Append(">");
                        if (question.answers[0].text != null)
                            sb.Append(question.answers[0].text);
                        sb.Append("</textarea>\n");
                        break;
                }
                sb.AppendLine("</p>");
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</form>");
            return sb.ToString();
        }
    }
}
