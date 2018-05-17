using Art_Logic.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace Art_Logic.Controllers
{
    public class ConversionController : Controller
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult Convert()
        {
            return View();
        }

        /// <summary>
        /// Handles the following:
        /// 1) Conversion of values within a text file from signed integers in the 14-bit range [-8192..+8191] to it's hexadecimal variant, or vise versa
        /// 2) Conversion of a signed integer in the 14-bit range [-8192..+8191] to it's hexadecimal variant
        /// 3) Conversion of a hexadecimal in the range [0x0000..0x7F7F] to it's signed integer in the 14-bit range [-8192..+8191] variant
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Convert(IntToHex model)
        {

            if (!ModelState.IsValid)
            {
                ViewBag.Error = true;

                return View(model);
            }

            try
            {
                if (model.File != null)
                {
                    ConvertFile(model.File);
                }
                else if (model.IntToConvert != null)
                {
                    ConvertInt((int)model.IntToConvert);
                }
                else if (model.HexToConvert != null)
                {
                    ConvertHex(model.HexToConvert);
                } 
            }
            catch (Exception e)
            {
                ViewBag.Exception = true;
                ViewBag.ExceptionMessage = e.Message;
                ViewBag.InnerExceptionMessage = e.InnerException.Message;
            }

            // if reached there was an Exception         
            return View(model);
        }

        /// <summary>
        /// Iterates through each line in a text file, converting the value on the line
        /// to either an Integer from a Hexadecimal or a Hexadecimal from an Integer
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private ActionResult ConvertFile(HttpPostedFileBase file)
        {
            string path = Server.MapPath(@"/Conversions/" + Path.GetFileNameWithoutExtension(file.FileName) + " - Converted.txt");
            string conversion;
            bool valid;

            //create the file to write the converted results
            System.IO.StreamWriter sw = new System.IO.StreamWriter(path);

            //to hold each line within the uploaded file
            List<string> lines = new List<string>();

            //read all the lines in the file
            using (System.IO.StreamReader reader = new System.IO.StreamReader(file.InputStream))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }

            //iterate through each line
            foreach (string line in lines)
            {
                //if the line is an integer
                if (int.TryParse(line, out int integer))
                {
                    //make sure the line is a valid integer in the 14 bit range
                    valid = integer > -8192 && integer <= 8191 ? true : false;

                    if (valid)
                    {
                        //convert to it's hexadecimal value
                        conversion = Encode(integer);
                    }
                    else
                    {
                        conversion = line + " is an invalid integer, valid 14-bit integer range is [-8192..+8191]";
                    }

                }
                else
                {
                    //make sure the line is a valid hexadecimal in the 14-bit range
                    valid = Int32.TryParse(line, System.Globalization.NumberStyles.HexNumber, null, out int myInt);

                    if (valid && myInt > 0 && myInt <= 16383)
                    {
                        //otherwise, convert from hexadecimal to integer
                        conversion = Decode(line.Substring(0, 2), line.Substring(2, 2)).ToString();
                    }
                    else
                    {
                        conversion = line + " equates to a hexadecimal value out of the valid 14-bit integer range [-8192..+8191]";
                    }

                }

                //write the converted data to the file
                sw.WriteLine(conversion);
            }

            //close the file stream
            sw.Close();

            ViewBag.FileConverted = true;
            ViewBag.ValToConvert = file.FileName;
            ViewBag.Result = Path.GetFileNameWithoutExtension(path);
            ViewBag.Location = path;

            //open the newly created, converted file for the user
            System.Diagnostics.Process.Start(path);

            return View("Convert");
        }

        /// <summary>
        /// Helper method to handle the conversion of an integer to a hexadecimal
        /// </summary>
        /// <param name="intToConvert"></param>
        /// <returns></returns>
        private ActionResult ConvertInt(int intToConvert)
        {

            string result;

            // convert to it's hexadecimal value
            result = Encode(intToConvert);

            ViewBag.IntConverted = true;
            ViewBag.ValToConvert = intToConvert;
            ViewBag.Result = result;

            return View("Convert");
        }

        /// <summary>
        /// Helper method to handle the conversion of a hexadecimal to an integer
        /// </summary>
        /// <param name="hexToConvert"></param>
        /// <returns></returns>
        private ActionResult ConvertHex(string hexToConvert)
        {
            string hiByte, loByte;
            short result;

            // rewrite the hexadecimal value with 4 characters for readability
            if (hexToConvert.Length == 1)
            {
                hexToConvert = "000" + hexToConvert;
            }
            else if (hexToConvert.Length == 2)
            {
                hexToConvert = "00" + hexToConvert;
            }
            else if (hexToConvert.Length == 3)
            {
                hexToConvert = "0" + hexToConvert;
            }

            // grab the first two characters of the hexadecimal
            hiByte = hexToConvert.Substring(0, 2);
            // grab the second two characters of the hexadecimal
            loByte = hexToConvert.Substring(2, 2).ToString();

            // convert from hexadecimal to integer
            result = Decode(hiByte, loByte);

            // check if the hexadecimal was in the valid range
            if(result > -8192 && result <= 8191)
            {
                ViewBag.HexConverted = true;
                ViewBag.ValToConvert = hexToConvert;
                ViewBag.Result = result;
            }
            else
            {
                ViewBag.Error = true;
            }

            return View("Convert");
        }

        /// <summary>
        /// Encode a signed integer in the 14-bit range  [-8192..+8191] and return a 4 character hexadecimal string
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string Encode(int number)
        {
            int translation, lowSevenBits, highSevenBits, combinedTwoByteValue, packedHighBits;
            string result;

            translation = number + 8192;


            highSevenBits = translation & 0x3F80; // 0011 1111 1000 0000 - ensuring we keep the highest 7 bits except for the most signficant
            lowSevenBits = translation & 0x007F; // 0000 0000 0111 1111 - ensuring we keep the lowest 7 bits except for the most significant

            // fit the highest 7 bits into the left byte utilizing a bitwise shift left by one, clearing the most significant bit of the right byte
            // add the two sets of bits together to get the combined two byte value
            combinedTwoByteValue = lowSevenBits + (highSevenBits << 1);

            // format the two byte value as a single 4-character hexadecimal string
            result = combinedTwoByteValue.ToString("X");

            return result;
        }

        /// <summary>
        /// Decode 4 character hexadecimal string to a signed integer in the 14-bit range [-8192..+8191]
        /// </summary>
        /// <param name="highByte"></param>
        /// <param name="lowByte"></param>
        /// <returns></returns>
        public static short Decode(string highByte, string lowByte)
        {
            byte low, high;
            short result, conversion;

            // convert the high and low strings to bytes from a base-16 representation of a number aka their Hexadecimal Value
            high = System.Convert.ToByte(highByte, 16);
            low = System.Convert.ToByte(lowByte, 16);

            // retrieve the highest 7 bits, except the most significant, in the left byte - combined with the low bits
            // combine the two bytes to retrieve the translated integer
            conversion = (short)(low + (high << 7));

            // untranslate the integer
            result = (short)(conversion - 8192);

            return result;
        }

    }
}
