using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSkassa
{
    class Kasbi02MF : KKM
    {
        //Свойства
        public override string SerialNumber { get; set; }
        public override string Name { get; set; }
        public RSConnect Connection { get; set; }
        public Query LastQuery { get; protected set; }
        public Query LastAnswer { get; protected set; }
        public QueriesTemplate TQueriesAndAnswers;
        public int NumberOfKKM;
        private int sleepPeriodCloseCheck = 200;

        //Методы
        public override void Init(int numberOfKKM = 1)
        {
            NumberOfKKM = numberOfKKM;
            Connection = new RSConnect();
            LastQuery = new Query();
            LastAnswer = new Query();

            try
            {
                TQueriesAndAnswers = JsonConvert.DeserializeObject<QueriesTemplate>(File.ReadAllText(Program.TSSettings.QueriesFile, Program.TSSettings.MainEncoding));
            }
            catch (JsonException e)
            {
                Program.ExitProgram("ERROR:" + e.Message);
            }

            Ping();
            if (LastAnswer.GetAnswerString() == "OK")
            {
                SetSerialNumber();
            }
        }

        public override void PrintCheck(string path)
        {
            if (!IsSmenaOpened())
            {
                Program.ExitProgram("ERROR:Закрыта смена!");
            }

            Program.MainLog.WriteLog("Печать чека НАЧАЛО");
            Check Check = new Check(path);
            try
            {
                ClearBuffer();
            }
            catch (Exception e)
            {
                Program.MainLog.WriteLog(e.Message);
            }


            PressButton(0x43);

            try
            {
                ClearBuffer();
            }
            catch (Exception e)
            {
                Program.MainLog.WriteLog(e.Message);
            }

            Section[] allStrings = Check.GetAllStrings();
            for (int i = 0; i < allStrings.Length; i++)
            {
                FillBufferEx(allStrings[i]);
            }
            Section Footer;
            Check.Sections.TryGetValue("FOOTER", out Footer);
            int stringN = 0;
            foreach (var KeyVal in Footer.ListKeyValue)
            {
                stringN++;
                PrintAdditionalInfo(stringN, KeyVal.Value);
            }

            CloseCheck(Check, allStrings.Length);

            GetStatusKKM();
            byte status;
            bool isNonClosedSales;
            bool isOpenedCheck;
            try
            {
                status = LastAnswer.Data[4];
                isNonClosedSales = (status & (1 << 5 - 1)) != 0;
                isOpenedCheck = (status & (1 << 3 - 1)) != 0;
                if (isOpenedCheck || isNonClosedSales)
                {
                    ClearBuffer();
                }
                GetStatusKKM();
                status = LastAnswer.Data[4];
                isOpenedCheck = (status & (1 << 3 - 1)) != 0;
            }
            catch (Exception e)
            {
                Program.MainLog.WriteLog(EVENTS.ERROR_GET_STATUS + " " + e.Message);
            }

            //Сдача
            string change = "Сдача:";
            //if (LastAnswer.Data.Length > 6)
            //{
            //    Program.MainLog.WriteLog("Начало вычисления сдачи");
            //    try
            //    {
            //        Converter Conv = new Converter();

            //        byte[] buffer = new byte[4];
            //        Array.Copy(LastAnswer.Data, 21, buffer, 0, 4);
            //        ulong AmountFromClient = Conv.ToULong(buffer);
            //        buffer = new byte[4];
            //        Array.Copy(LastAnswer.Data, 16, buffer, 0, 4);
            //        ulong AmountCheck = Conv.ToULong(buffer);
            //        string strChange = (AmountFromClient - AmountCheck).ToString();

            //        for (int i = 0; i < 10 - strChange.Length; i++)
            //        {
            //            change += " ";
            //        }

            //        if (strChange.Length <= 10)
            //        {
            //            change += strChange;
            //        }
            //        else
            //        {
            //            change += strChange.Substring(0, 10);
            //        }
            //        Program.MainLog.WriteLog("Конец вычисления сдачи");
            //        ShowMessage(0x00, change);
            //        ShowMessage(0x10, change);
            //    }
            //    catch (Exception e)
            //    {
            //        Program.MainLog.WriteLog(e.Message);
            //    }
            //}

            Section itog;
            Check.Sections.TryGetValue("ITOG", out itog);
            string changeVal;
            itog.ListKeyValue.TryGetValue("Change", out changeVal);

            if (changeVal == null)
            {
                changeVal = "0";
            }

            changeVal = changeVal.Trim();

            for (int i = 0; i < 16 - change.Length - changeVal.Length; i++)
            {
                change += " ";
            }

            float tempVal = 0;
            Single.TryParse(changeVal, System.Globalization.NumberStyles.Currency, new System.Globalization.CultureInfo("en-US"), out tempVal);
            if (tempVal == 0)
            {
                change = "Без сдачи";
            }
            else
            {
                change += changeVal;
            }
            //Показать сдачу
            ShowMessage(0x00, change);
            ShowMessage(0x10, change);


            //PressButton(0x41); // Итог

            //Section Itog;
            //Check.Sections.TryGetValue("ITOG", out Itog);
            //string Payment0 = "";
            //Itog.ListKeyValue.TryGetValue("Payment0", out Payment0);
            //foreach (char Symb in Payment0)
            //{
            //    PressButton((int)Symb);
            //}

            //PressButton(0x40); //BB

            //System.Threading.Thread.Sleep(5000);
            //GetSmenaInformation();

            //if (LastAnswer.Data.Length > 137)
            //{
            //    string CheckNumber;
            //    byte[] checkNumberArray = new byte[2];
            //    checkNumberArray[0] = LastAnswer.Data[136];
            //    checkNumberArray[1] = LastAnswer.Data[137];
            //    CheckNumber = checkNumberArray[1].ToString();
            //    Program.Log1C.WriteLog("OK| LASTCHECK:"+CheckNumber+"\nSERIAL:" + SerialNumber);
            //}

            Program.MainLog.WriteLog("Печать чека КОНЕЦ");
        }

        public void GetStatusKKM()
        {
            Program.MainLog.WriteLog("Запрос статуса ККМ");

            QueryFillingFields queryFields = new QueryFillingFields(21);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 0;

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer, 100);
            LastAnswer = new Query(answer, TQueriesAndAnswers);
            Program.MainLog.WriteLog("Конец запроса статуса ККМ");
        }

        public bool IsSmenaOpened()
        {
            bool retV = false;

            Program.MainLog.WriteLog("Получение статуса смены ");

            QueryFillingFields queryFields = new QueryFillingFields(21);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 0;

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer);
            LastAnswer = new Query(answer, TQueriesAndAnswers);

            byte status = LastAnswer.Data[4];
            retV = (status & (1 << 1 - 1)) != 0;

            if (retV)
            {
                Program.MainLog.WriteLog("Смена открыта");
            }
            else
            {
                Program.MainLog.WriteLog("Смена Закрыта");
            }
            

            return retV;
        }

        public void GetSmenaInformation()
        {

            Program.MainLog.WriteLog("Получение полной информации о итогах за смену ");

            System.Threading.Thread.Sleep(250);

            QueryFillingFields queryFields = new QueryFillingFields(25);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 0;

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer, 200);
            LastAnswer = new Query(answer, TQueriesAndAnswers);
            Program.MainLog.WriteLog("Конец получения полной информации об итогах за смену");
        }

        public void PrintAdditionalInfo(int number, string str)
        {
            Program.MainLog.WriteLog("Печать футера ");

            QueryFillingFields queryFields = new QueryFillingFields(12);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 27;
            queryFields.StringNumber = number-1;
            queryFields.AddInfo = str;

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer);
            LastAnswer = new Query(answer, TQueriesAndAnswers);
            Program.MainLog.WriteLog("Печать футера - " + LastAnswer.GetAnswerString());
        }

        public void PressButton(int ScanCode)
        {
            Program.MainLog.WriteLog("Нажатие клавиши " + ScanCode.ToString());

            QueryFillingFields queryFields = new QueryFillingFields(19);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 1;
            queryFields.ScanCode = ScanCode;

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer);
            LastAnswer = new Query(answer, TQueriesAndAnswers);
            Program.MainLog.WriteLog("Нажатие клавиши - " + LastAnswer.GetAnswerString());
        }

        public override void ClearBuffer()
        {
            Program.MainLog.WriteLog("Очистка буфера");

            QueryFillingFields queryFields = new QueryFillingFields(17);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 0;

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer);
            LastAnswer = new Query(answer, TQueriesAndAnswers);
            Program.MainLog.WriteLog("Очистка буфера завершена - " + LastAnswer.GetAnswerString());
        }
        public override void ClearBuffer(object Buffer)
        {
            Program.MainLog.WriteLog("Очистка покупки в чековом буфере");

            QueryFillingFields queryFields = new QueryFillingFields(16);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 1;
            queryFields.StringNumber = (int)Buffer;

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer);
            LastAnswer = new Query(answer, TQueriesAndAnswers);
            Program.MainLog.WriteLog("Очистка буфера завершена - " + LastAnswer.GetAnswerString());
        }
        public override void ClearBuffer(object[] Buffers)
        {
            Program.MainLog.WriteLog("Очистка покупок(массив) в чековом буфере");

            QueryFillingFields queryFields = new QueryFillingFields(16);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 1;
            for (int i = 0; i < Buffers.Length; i++)
            {
                queryFields.StringNumber = (int)Buffers[i];

                LastQuery = GetQuery(queryFields);
                byte[] answer = new byte[0];
                Connection.SendToPort(LastQuery.Data, ref answer);
                LastAnswer = new Query(answer, TQueriesAndAnswers);
                Program.MainLog.WriteLog("Очистка буфера завершена - " + LastAnswer.GetAnswerString());
            }
        }

        public override void FillBufferEx(object BufferData)
        {
            Section data = (Section)BufferData;
            Program.MainLog.WriteLog("Запись буфера");

            QueryFillingFields queryFields = new QueryFillingFields(50);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 84;
            //Заполним нужными данными
            FillSourceWithData(data.ListKeyValue, queryFields);

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer);
            LastAnswer = new Query(answer, TQueriesAndAnswers);
            Program.MainLog.WriteLog("Конец записи буфера - " + LastAnswer.GetAnswerString());
        }

        private void FillSourceWithData(IDictionary<string, string> data, QueryFillingFields source)
        {
            foreach (var KeyVal in data)
            {
                string ValueStr = "";
                data.TryGetValue(KeyVal.Key, out ValueStr);
                var field = source[KeyVal.Key];
                var type = field.GetType();
                if (type == typeof(int))
                {
                    float tempV = Convert.ToSingle(ValueStr, new System.Globalization.CultureInfo("en-US"));
                    if (KeyVal.Key == "Quantity")
                    {
                        tempV = tempV * 1000;
                    }
                    source[KeyVal.Key] = Int32.Parse(tempV.ToString());
                }
                else if (type == typeof(Int64))
                {
                    source[KeyVal.Key] = Double.Parse(ValueStr, System.Globalization.NumberStyles.Currency, new System.Globalization.CultureInfo("en-US"));
                }
                else if (type == typeof(float)) {
                    float tempV = Convert.ToSingle(ValueStr, new System.Globalization.CultureInfo("en-US"));
                    source[KeyVal.Key] = tempV;
                } else {
                    source[KeyVal.Key] = ValueStr;
                }
            }
        }

        private void CloseCheck(Check check, int NumberOfSales)
        {
            Program.MainLog.WriteLog("Закрытие чека");
            QueryFillingFields queryFields = new QueryFillingFields(18);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 5;
            //Заполним нужными данными
            Section itog;
            check.Sections.TryGetValue("ITOG", out itog);
            //FillSourceWithData(itog.ListKeyValue, queryFields);
            string Payment0;
            if (!itog.ListKeyValue.TryGetValue("Payment0", out Payment0))
            {
                Payment0 = "";
            }
            string Payment1;
            if (!itog.ListKeyValue.TryGetValue("Payment1", out Payment1))
            {
                Payment1 = "";
            }

            float lPayment0;
            float lPayment1;
            //long iPayment2 = 0;
            //long iPayment3 = 0;
            if (!Single.TryParse(Payment0, System.Globalization.NumberStyles.Currency, new System.Globalization.CultureInfo("en-US"), out lPayment0))
            {
                lPayment0 = 0;
            }
            if (!Single.TryParse(Payment1, System.Globalization.NumberStyles.Currency, new System.Globalization.CultureInfo("en-US"), out lPayment1))
            {
                lPayment1 = 0;
            }

            //long lAmount = 0;

            if (lPayment0 != 0)
            {
                queryFields.OperationType = 0;
                queryFields.Amount = lPayment0;
            }
            else if (lPayment1!=0)
            {
                queryFields.OperationType = 1;
                queryFields.Amount = lPayment1;
            }
            else
            {
                throw new PaymentException("ERROR: Ошибка получения типа платежа при закрытии чека");
            }
            //queryFields.OperationType = 0;
            //queryFields.Amount = Int64.Parse(Payment0);

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer, NumberOfSales * sleepPeriodCloseCheck);

            if (Program.TSSettings.TestMode)
            {
                answer = new byte[31] { 1, 27, 18, 1, 0, 13, 0, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 10, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0, 0 };
                answer[30] = Connection.CalcCRC(answer);
            }



            LastAnswer = new Query(answer, TQueriesAndAnswers);
            if (LastAnswer.Size > 1)
            {
                Program.MainLog.WriteLog("Конец закрытия чека - " + "OK");

                string LastCheck;
                byte[] LastCheckBytes = new byte[2] { LastAnswer.Data[4], LastAnswer.Data[5] };
                Converter conv = new Converter();
                LastCheck = (conv.ToULong(LastCheckBytes)-1).ToString();
                if (Program.TSSettings.TestMode)
                {
                    LastCheck = "111";
                }
                //LastCheck = Program.TSSettings.MainEncoding.GetString(LastCheckBytes);
                Program.Log1C.WriteLog("OK| LASTCHECK:" + LastCheck + "\nSERIALNUMBER:" + SerialNumber);
                Program.MainLog.WriteLog("OK| LASTCHECK:" + LastCheck + "\nSERIALNUMBER:" + SerialNumber);
            } else if (LastAnswer.GetAnswerString() == "OK") {
                Program.MainLog.WriteLog("Конец закрытия чека - " + "OK");

                string LastCheck = String.Empty;

                byte[] summaryAnswer = GetSummary();
                if (summaryAnswer.Length == 5)
                {
                    Program.Log1C.WriteLog("ERROR: Нет продаж. Проверьте открыта ли смена.");
                    Program.MainLog.WriteLog("ERROR: Нет продаж. Проверьте открыта ли смена.");
                } else
                {
                    byte[] lastCheckBytes = { 0, 0 };
                    Array.Copy(summaryAnswer, 136, lastCheckBytes, 0, 2);
                    Array.Reverse(lastCheckBytes);
                    var lastCheckNumber = BitConverter.ToInt16(lastCheckBytes, 0);
                    LastCheck = lastCheckNumber.ToString();
                    Program.Log1C.WriteLog("OK| LASTCHECK:" + LastCheck + "\nSERIALNUMBER:" + SerialNumber);
                    Program.MainLog.WriteLog("OK| LASTCHECK:" + LastCheck + "\nSERIALNUMBER:" + SerialNumber);
                }
            } else {
                Program.MainLog.WriteLog("Конец закрытия чека - " + LastAnswer.GetAnswerString() + " => нет последнего ответа");
            }
        }

        //Показывает сообщение на индикаторе информационной строки
        //0х00 - индикатор продавца 
        //0х00 - индикатор покупателя
        private void ShowMessage(int mon, string msg)
        {
            if (msg.Length > 16)
            {
                msg = msg.Substring(0, 16);
            }
            Program.MainLog.WriteLog("Вывод на индикатор строки: " + msg);

            QueryFillingFields queryFields = new QueryFillingFields(20);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 17;
            queryFields.IndicatorNumber = mon;
            queryFields.StringData = msg;

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer);
            LastAnswer = new Query(answer, TQueriesAndAnswers);
            Program.MainLog.WriteLog("Конец вывода" + LastAnswer.GetAnswerString());
        }

        private void Ping()
        {
            Program.MainLog.WriteLog("Ping...");

            QueryFillingFields queryFields = new QueryFillingFields(24);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 0;

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer);
            LastAnswer = new Query(answer, TQueriesAndAnswers);
            Program.MainLog.WriteLog("Pong..." + LastAnswer.GetAnswerString());
        }

        private void SetSerialNumber()
        {
            Program.MainLog.WriteLog("Получение серийного номера");

            QueryFillingFields queryFields = new QueryFillingFields(27);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 0;

            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer);
            LastAnswer = new Query(answer, TQueriesAndAnswers);

            try
            {
                byte[] serialBytes = new byte[LastAnswer.Data[1]];
                Array.Copy(LastAnswer.Data, 3, serialBytes, 0, (int)LastAnswer.Data[1]);

                SerialNumber = Encoding.ASCII.GetString(serialBytes);

            }
            catch (Exception e)
            {
                Program.MainLog.WriteLog(EVENTS.CANNOT_GET_SERIAL_NUMBER + " " + e.Message);
            }
            if (Program.TSSettings.TestMode == true)
            {
                SerialNumber = "1234";
            }
            Program.MainLog.WriteLog("Серийный номер: " + SerialNumber);
        }

        private Query GetQuery(QueryFillingFields queryFields)
        {
            string subquery = "q" + queryFields.Command.ToString() + "-" + queryFields.Size.ToString();
            LastQuery = new Query(queryFields.Command, TQueriesAndAnswers, false, subquery);

            LastQuery.FillData(queryFields);

            return LastQuery;
        }

        public override byte[] GetSummary()
        {
            Program.MainLog.WriteLog("Получение итогов за смену");

            QueryFillingFields queryFields = new QueryFillingFields(25);
            queryFields.NumberOfKKM = NumberOfKKM;
            queryFields.Size = 0;


            LastQuery = GetQuery(queryFields);
            byte[] answer = new byte[0];
            Connection.SendToPort(LastQuery.Data, ref answer);
            LastAnswer = new Query(answer, TQueriesAndAnswers);

            Program.MainLog.WriteLog(LastAnswer.GetAnswerString());

            return answer;
        }

    }

    //Коды ошибок
    public class KKMErrors
    {
        public IDictionary<int, string> CodeError;
        public KKMErrors()
        {
            CodeError = new Dictionary<int, string>();
            CodeError.Add(0, "OK");
            CodeError.Add(1, "Не хватает наличности при оплате чека");
            CodeError.Add(2, "Не хватает наличных в кассе при выполнении возврата и изъятия");
            CodeError.Add(3, "Нет соответствия ставкам ККМ");
            CodeError.Add(4, "Переполнение буфера покупок");
            CodeError.Add(5, "Нет продаж");
            CodeError.Add(6, "Ошибка 24 часа, переполнение записей в КЛ");
            CodeError.Add(7, "Нет товара с таким кодом");
            CodeError.Add(8, "Код товара превысил максимально возможный");
            CodeError.Add(9, "Есть продажи по данному товару");
            CodeError.Add(10, "Не верно задан адрес или количество байт при записи или чтении массива программируемых параметров(при чтении РПЗУ)");
            CodeError.Add(11, "Операция запрещена при открытой смене");
            CodeError.Add(12, "Переполнение суммы продаж");
            CodeError.Add(13, "Неверный формат данных");
            CodeError.Add(14, "Нет бумаги");
            CodeError.Add(15, "ККМ необходимо перевести в режим \"КАССА\"");
            CodeError.Add(16, "Несуществующий номер продаж");
            CodeError.Add(17, "Закрыть КЛ");
        }
    }

    //Класс, описывающий поля для заполнения запроса
    class QueryFillingFields
    {
        public int NumberOfKKM { get; set; }
        public int Size { get; set; }
        public int Command { get; set; }
        public string SubCommand { get; set; } = "";
        //BEGIN
        public string Adress { get; set; } = "";
        public int ErrorCode { get; set; }
        public int StringNumber { get; set; }
        public string AddInfo { get; set; } = "";
        public int OperationType { get; set; }
        public float Amount { get; set; }
        public int QuantityOfBuy { get; set; }
        public int CheckNumber { get; set; }
        public int ShiftNumber { get; set; }
        public DateTime Date { get; set; }
        public DateTime Time { get; set; }
        public int Cashier { get; set; }
        public int HighByteOfPayment { get; set; }
        public float AmountCheck { get; set; }
        public int ReserveMode { get; set; }
        public float AmountClient { get; set; }
        public int QuantityStorno { get; set; }
        public float AmountStorno { get; set; }
        public int ScanCode { get; set; }
        public int IndicatorNumber { get; set; }
        public string StringData { get; set; } = "";
        public int Mode { get; set; }
        public int Status { get; set; }
        public string AddDescription { get; set; } = "";
        public byte[] Data { get; set; }
        public string SerialKKM { get; set; } = ""; //только чтение!
        public float ShiftAmount { get; set; }
        public int Department { get; set; }
        public int Quantity { get; set; }
        public float Price { get; set; }
        public int Discount { get; set; } //В процентах
        public int DiscountMode { get; set; }
        public string BarCode { get; set; } = "";
        public string Description { get; set; } = "";
        //END

        public QueryFillingFields(int query)
        {
            Command = query;
        }

        public object this[string propertyName]
        {
            get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }
    }

    //Класс, описывающий запрос.
    class Query
    {
        public DateTime Date { get; private set; }
        public int Command { set; get; }
        public string SubCommand { set; get; }
        public string Name { set; get; }
        public int Size { set; get; }
        public byte[] Data { get; set; }
        FieldDefinition[] FieldsDef { get; set; }

        public Query() { }

        public Query(int query, QueriesTemplate queryTemplate, bool isAnswer = false, string subquery = "")
        {
            GetFieldsDefinition(query, queryTemplate, false, subquery);
        }

        public Query(byte[] answerArray, QueriesTemplate queryTemplate)
        {
            if (answerArray.Length < 3)
            {
                Program.ExitProgram(EVENTS.ERROR_EMPTY_ANSWER);
            }      
            try
            {
                string subquery = "q" + answerArray[2].ToString() + "-" + answerArray[1].ToString();
                GetFieldsDefinition((int)answerArray[2], queryTemplate, true, subquery);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Data = answerArray;

        }

        private void GetFieldsDefinition(int query, QueriesTemplate queryTemplate, bool isAnswer, string subquery)
        {
            QueriesAndAnswers tempQA = new QueriesAndAnswers();
            if (queryTemplate.QueriesKasbi02MF.TryGetValue(query, out tempQA))
            {
                IDictionary<string, QueryDefinition> allQueries;
                if (isAnswer)
                {
                    allQueries = tempQA.Answers;
                }
                else
                {
                    allQueries = tempQA.Queries;
                }
                if (allQueries.Count < 1)
                {
                    Program.ExitProgram(EVENTS.ERROR_IN_QUERY_TEMPLATE);
                }

                QueryDefinition tempQDef = new QueryDefinition();
                if (allQueries.Count == 1 || subquery == "")
                {
                    var KeyVal = allQueries.First();
                    tempQDef = KeyVal.Value;
                }
                else
                {
                    if (!allQueries.TryGetValue(subquery, out tempQDef))
                    {
                        Program.ExitProgram(EVENTS.ERROR__GETTING_SUBCOMMAND);
                    }
                }

                FieldsDef = tempQDef.bytesDef.ToArray();

                Name = tempQDef.Name;
                Size = tempQDef.Size;
                Command = tempQDef.Command;
                if (subquery != "")
                {
                    SubCommand = subquery;
                }
            }
        }

        public void FillData(QueryFillingFields source)
        {
            int curIndex = 0;
            //Посчитаем количество байт в запросе;
            int bytesCount = 0;
            for (int i = 0; i < FieldsDef.Length; i++)
            {
                bytesCount += FieldsDef[i].SizeInBytes;
            }

            Data = new byte[bytesCount];
            for (int i = 0; i < FieldsDef.Length; i++)
            {
                var field = FieldsDef[i];
                if (FieldsDef[i].ParameterName == "crc")
                {
                    Data[Data.Length - 1] = CalcCRC(Data);
                }
                else
                {
                    FillDataWithProperty(ref curIndex, field, source);
                }

            }
        }

        private void FillDataWithProperty(ref int curIndex, FieldDefinition fieldDef, QueryFillingFields source)
        {
            object Value = null;
            try
            {
                //if (fieldDef.ParameterName == "AddDescription")
                //{
                //    Value = source.AddDescription;
                //}
                //else
                //{
                    Value = source[fieldDef.ParameterName];
                //}
            }
            catch (Exception e)
            {
                Program.ExitProgram(EVENTS.ERROR_FILLING_QUERY + " " + e.Message);
            }

            switch (fieldDef.Type)
            {
                case FieldType.is1000Int:
                    FillInt(curIndex, (int)Value, fieldDef);
                    break;
                case FieldType.isInt:
                    if (fieldDef.SizeInBytes == 4)
                    {
                        FillInt(curIndex, (long)Value, fieldDef);
                    }
                    else
                    {
                        FillInt(curIndex, (int)Value, fieldDef);
                    }

                    break;
                case FieldType.isString:
                    FillString(curIndex, (string)Value, fieldDef);
                    break;
                case FieldType.isDate:
                    break;
                case FieldType.isTime:
                    break;
                case FieldType.isFloat:
                    FillFloat(curIndex, (float)Value, fieldDef);
                    break;
                default:
                    Program.ExitProgram(EVENTS.ERROR_FILLING_QUERY);
                    break;
            }

            curIndex += fieldDef.SizeInBytes;
        }

        private void FillFloat(int startIndex, float value, FieldDefinition fieldDef)
        {
            byte[] byteArray = BitConverter.GetBytes((int)Math.Round((decimal)(value*100)));
            byteArray = ResizeByteArray(byteArray, fieldDef.SizeInBytes);

            for (int i = 0; i < byteArray.Length; i++)
            {
            //    //if (fieldDef.Order == FieldOrder.straight)
            //    //{
            //    //    Data[startIndex + i] = byteArray[i];
            //    //}
            //    //else if (fieldDef.Order == FieldOrder.reverse)
            //    //{
                    Data[startIndex + byteArray.Length - 1 - i] = byteArray[i];
            //    //}
            }
            //Data[startIndex] = 0;
            //Data[startIndex + 1] = 0;
            //Data[startIndex + 2] = 0x80;
            //Data[startIndex + 3] = 0x3F;
        }

        private void FillInt(int startIndex, long value, FieldDefinition fieldDef)
        {
            byte[] byteArray = BitConverter.GetBytes(value);
            byteArray = ResizeByteArray(byteArray, fieldDef.SizeInBytes);

            for (int i = 0; i < byteArray.Length; i++)
            {
                if (fieldDef.Order == FieldOrder.straight)
                {
                    Data[startIndex + i] = byteArray[i];
                }
                else if (fieldDef.Order == FieldOrder.reverse)
                {
                    Data[startIndex + byteArray.Length - 1 - i] = byteArray[i];
                }
            }
        }

        private void FillString(int startIndex, string value, FieldDefinition fieldDef)
        {
            byte[] byteArray = Program.TSSettings.MainEncoding.GetBytes(value);
            byteArray = ResizeByteArray(byteArray, fieldDef.SizeInBytes);

            for (int i = 0; i < byteArray.Length; i++)
            {

                if (byteArray[i] == 0)
                {
                    if (fieldDef.IsZeroSpace)
                    {
                        byteArray[i] = 32;
                    }
                    else
                    {
                        byteArray[i] = 48;
                    }
                }


            }

            for (int i = 0; i < byteArray.Length; i++)
            {
                if (fieldDef.Order == FieldOrder.straight)
                {
                    Data[startIndex + i] = byteArray[i];
                }
                else if (fieldDef.Order == FieldOrder.reverse)
                {
                    Data[startIndex + byteArray.Length - 1 - i] = byteArray[i];
                }
            }
        }

        private byte[] ResizeByteArray(byte[] array, int newSize, bool addZerosToBegin = false)
        {
            if (array.Length == newSize)
            {
                return array;
            }
            byte[] retArray = array;
            Array.Resize(ref retArray, newSize);
            return retArray;
        }

        private byte CalcCRC(byte[] mas)
        {
            byte crc = mas[0];

            for (int i = 1; i < mas.Length; i++)
                crc = (byte)(crc + mas[i]);

            return (byte)(crc ^ 0xFF);
        }

        public void LoadAnswerData(byte[] byteArray, Query LastQuery)
        {
            Data = byteArray;
            SetDate();

        }

        public string GetAnswerString()
        {
            string retStr = EVENTS.ERROR_EMPTY_ANSWER;

            KKMErrors errors = new KKMErrors();
            try
            {
                int code = (int)Data[3];
                errors.CodeError.TryGetValue(code, out retStr);
            }
            catch (Exception e)
            {
                Program.ExitProgram(EVENTS.ERROR_IN_ANSWER_FROM_KKM + " " + e.Message);
            }

            return retStr;
        }

        public void SetDate()
        {
            Date = DateTime.Now;
        }

        public override string ToString()
        {
            string str = "";
            for (int i = 0; i < Data.Length; i++)
            {
                str = str + Data[i] + " ";
            }
            return str.Trim();
        }
    }


    //Классы, описывающие структуру запросов!
    class QueriesTemplate
    {
        public IDictionary<int, QueriesAndAnswers> QueriesKasbi02MF { get; set; }
    }

    class QueriesAndAnswers
    {
        public IDictionary<string, QueryDefinition> Queries { get; set; }
        public IDictionary<string, QueryDefinition> Answers { get; set; }
    }


    class QueryDefinition
    {
        public int Command { get; set; }
        public int Size { get; set; }
        public string Name { get; set; }
        public IList<FieldDefinition> bytesDef { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FieldType { isInt, is1000Int, isString, isDate, isTime, isFloat }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FieldOrder { straight, reverse }

    public class FieldDefinition
    {
        public string Name { get; set; }
        public string ParameterName { get; set; }
        public FieldType Type { get; set; }
        public int SizeInBytes { get; set; }
        public FieldOrder Order { get; set; }
        public bool IsZeroSpace { get; set; }
    }

    class AnswerManagement
    {
        public Query Answer;

        public AnswerManagement(Query query)
        {
            Answer = query;
        }
    }
}
