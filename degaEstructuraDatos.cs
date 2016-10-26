using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Data;
using System.Collections;
using System.Data.SqlClient;
using System.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;

namespace dega
{
    /// <summary>
    /// 
    /// Helper for DAL using EnterpriseLibrary6 AppBlockData
    /// for one line invokes stored procedures
    /// 
    /// Daniel González Aguirre 
    /// dega@dega.cl
    /// dgonzalezaguirre@dega.cl
    /// 
    /// </summary>
    public class degaEstructuraDatos
    {
        private Database dsn;
        private SecurityConnectionType securityConnectionType = SecurityConnectionType.Unconfigured;

        private enum SecurityConnectionType
        {
            All,
            Password,
            Unconfigured
        }

        public degaEstructuraDatos()
        {

            evaluateSecurityConnectionType();

            if ((ConfigurationManager.ConnectionStrings["dsn"] == null) && (securityConnectionType != SecurityConnectionType.Unconfigured))
            {
                dsn = new SqlDatabase(getConnectionString());
            }
            else
            {
                dsn = new DatabaseProviderFactory().Create("dsn");
            }

        }

        public degaEstructuraDatos(string connectionString)
        {
            dsn = new SqlDatabase(connectionString);
        }

        private string getConnectionString()
        {

            string mConnectionString = string.Empty;

            try
            {

                if (securityConnectionType == SecurityConnectionType.Unconfigured)
                {
                    mConnectionString = ConfigurationManager.ConnectionStrings["dsn"].ConnectionString;
                }

                if (securityConnectionType == SecurityConnectionType.All)
                {
                    mConnectionString = encdec.DecryptStringAES(ConfigurationManager.AppSettings["dsn"], "dega");
                }

                if (securityConnectionType == SecurityConnectionType.Password)
                {
                    mConnectionString = ConfigurationManager.ConnectionStrings["dsn"].ConnectionString;
                    SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(mConnectionString);
                    csb.Password = encdec.DecryptStringAES(csb.Password, "dega");
                    //csb.UserID = encdec.DecryptStringAES(csb.UserID, "dega2");
                    //csb.DataSource = encdec.DecryptStringAES(csb.DataSource, "dega3");
                    //csb.InitialCatalog = encdec.DecryptStringAES(csb.InitialCatalog, "dega4");

                    mConnectionString = csb.ToString();
                }

            }catch { }

            return mConnectionString;



        }

        private void evaluateSecurityConnectionType()
        {
            string R = string.Empty;

            try
            {
                R = ConfigurationManager.AppSettings["SecurityConnectionType"];
            }
            catch
            {
            }

            if (R == string.Empty || R == null)
            {
                securityConnectionType = SecurityConnectionType.Unconfigured;
                return;
            }

            if (R.Trim().ToLower() == "all") securityConnectionType = SecurityConnectionType.All;
            if (R.Trim().ToLower().StartsWith("pass")) securityConnectionType = SecurityConnectionType.Password;



        }





        #region "Get Estructuras Datos"

        //public void Ejecuta(string NombrePA, params object[] Parametros)
        //{
        //    try
        //    {
        //        dsn.ExecuteNonQuery(NombrePA, Parametros);
        //    }
        //    catch (Exception e)
        //    {
        //        System.Diagnostics.Debug.Print(e.Message);
        //    }

        //}

        public int GuardarBinario(byte[] binario, string Tabla, string NombreCampo, string NombreID, int ValorID)
        {

            string strSQL = "UPDATE " + Tabla + " SET " + NombreCampo + " = @binaryValue " + " WHERE " + NombreID + " = " + ValorID.ToString();

            SqlConnection conexion = new SqlConnection(dsn.ConnectionString);

            conexion.Open();

            using (SqlCommand cmd = new SqlCommand(strSQL, conexion))
            {
                cmd.Parameters.Add("@binaryValue", SqlDbType.VarBinary).Value = binario;
                cmd.ExecuteNonQuery();
            }

            conexion.Close();


            return 0;

        }


        public List<DataRow> GetListaFilas(string NombreProcedimientoAlmacenado, params object[] Parametros)
        {
            return getListaFilas(GetTabla(NombreProcedimientoAlmacenado, Parametros));
        }

        public List<DataRow> GetListaFilasTablaDirecta(string NombreTabla)
        {
            return getListaFilas(GetTablaDirecta(NombreTabla));
        }

        public List<DataRow> GetListaFilasSQL(string strSQL)
        {
            return getListaFilas(GetDataSetSQL(strSQL).Tables[0]);
        }

        public List<List<string>> GetListaTabla(string NombreProcedimientoAlmacenado, params object[] Parametros)
        {
            return getListaTabla(GetTabla(NombreProcedimientoAlmacenado, Parametros));
        }
        public List<List<string>> GetListaSQL(string strSQL)
        {
            return getListaTabla(GetDataSetSQL(strSQL).Tables[0]);
        }


        private List<List<string>> getListaTabla(DataTable dt)
        {

            List<List<string>> lstTable = new List<List<string>>();

            foreach (DataRow row in dt.Rows)
            {
                List<string> lstRow = new List<string>();
                foreach (var item in row.ItemArray)
                {
                    lstRow.Add(item.ToString().Replace("\r\n", string.Empty));
                }
                lstTable.Add(lstRow);
            }

            return lstTable;

        }

        private List<DataRow> getListaFilas(DataTable dt)
        {
            return dt.AsEnumerable().ToList();
            //IEnumerable<DataRow> sequence = dt.AsEnumerable();
        }


        public DataTable GetTabla(string NombrePA, params object[] Parametros)
        {
            //string R;
            DataSet ds = null;
            DataTable dt = null;

            try
            {
                ds = dsn.ExecuteDataSet(NombrePA, Parametros);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.Message);
            }
            try
            {
                dt = ds.Tables[0];
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return dt;
        }

        public DataTable GetTablaSQL(string SQL)
        {

            DataSet ds = null;
            DataTable dt = null;

            try
            {
                ds = GetDataSetSQL(SQL);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.Message);
            }
            try
            {
                dt = ds.Tables[0];
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return dt;
        }


        public DataTable GetTablaDirecta(string NombreTabla)
        {

            string SQL = "SELECT * FROM " + NombreTabla;

            DataSet ds = null;
            DataTable dt = null;

            try
            {
                ds = GetDataSetSQL(SQL);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.Message);
            }
            try
            {
                dt = ds.Tables[0];
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return dt;
        }

        public void EjecutaPA(string NombrePA, params object[] Parametros)
        {
            try
            {
                dsn.ExecuteNonQuery(NombrePA, Parametros);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.Message);
            }

        }

        public void Ejecuta(string SQL)
        {
            try
            {
                dsn.ExecuteNonQuery(CommandType.Text, SQL);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.Message);
            }

        }



        public DataSet GetDataSet(string NombrePA, params object[] Parametros)
        {

            DataSet ds = null;

            try
            {

                ds = dsn.ExecuteDataSet(NombrePA, Parametros);

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.Message);
            }


            return ds;

        }

        public DataSet GetDataSetSQL(string SQL)
        {

            DataSet ds = null;

            try
            {

                ds = dsn.ExecuteDataSet(CommandType.Text, SQL);

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.Message);
            }


            return ds;

        }

        public DataRow GetDatos(string NombrePA, params object[] Parametros)
        {

            // Devuelve datos PrimeraFila

            DataTable dt = null;
            DataRow dr = null;

            try
            {


                dt = GetTabla(NombrePA, Parametros);
                dr = dt.Rows[0];
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return dr;

        }

        public DataRow GetDatosSQL(string SQL)
        {

            // Devuelve datos PrimeraFila

            DataTable dt = null;
            DataRow dr = null;

            try
            {


                dt = GetTablaSQL(SQL);
                dr = dt.Rows[0];
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return dr;

        }
        public string GetEscalarString(string NombrePA, params object[] Parametros)
        {
            string R = "";

            try
            {
                R = dsn.ExecuteScalar(NombrePA, Parametros).ToString();
                //R = SqlHelper.ExecuteScalar(this.dsn, NombrePA, Parametros).ToString();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return R;
        }

        public int GetEscalarEntero(string NombrePA, params object[] Parametros)
        {
            int R = 0;

            try
            {
                R = Convert.ToInt32(dsn.ExecuteScalar(NombrePA, Parametros));
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return R;
        }

        public bool GetEscalarBoleano(string NombrePA, params object[] Parametros)
        {
            bool R = false;

            try
            {
                R = Convert.ToBoolean(dsn.ExecuteScalar(NombrePA, Parametros));
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return R;
        }

        public double GetEscalarDouble(string NombrePA, params object[] Parametros)
        {
            double R = 0;

            try
            {
                R = Convert.ToDouble(dsn.ExecuteScalar(NombrePA, Parametros));
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return R;
        }


        public string GetEscalarStringBytes(string NombrePA, params object[] Parametros)
        {
            string R = "";

            try
            {

                byte[] Rb = (byte[])dsn.ExecuteScalar(NombrePA, Parametros);
                R = Encoding.Default.GetString(Rb);


            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return R;
        }


        public byte[] GetBytesPA(string NombrePA, params object[] Parametros)
        {
            byte[] Rb = null;

            try
            {

                Rb = (byte[])dsn.ExecuteScalar(NombrePA, Parametros);


            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return Rb;
        }




        // LOG
        //
        // Primera versión marzo 2004
        // GetTabla, GetDataSet, GetDatos, GetEscalarEntero, GetEscalarString
        // 
        // 2006
        // GetEscalarBoleano, GetTablaDirecta, EjecutaPA, Ejecuta, GetDataSetSQL
        // 
        // 2008
        // GuardarBinario, GetBytesPA, GetEscalarStringBytes, GetEscalarDouble
        // 
        // 2014
        // Habilitación LinQ
        // GetListaFilas, GetListaFilasSQL, GetListaSQL
        // 




        #endregion
    }

}

