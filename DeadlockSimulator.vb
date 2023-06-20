
Imports System.Data.SqlClient
Imports System.Threading

Public Class DeadlockSimulator
  Private Shared connectionString As String = "YOUR_CONNECTION_STRING"
    Private Shared exceptions As List(Of Exception) = New List(Of Exception)()


    ''' <summary>
    ''' Method that throws a Deadlock exception every time.  This is meant to mimick this behavior to properly plan for logging and notification.
    ''' </summary>
    Public Shared Sub Main()
        ' Create two threads for concurrent transactions
        Dim thread1 As New Thread(AddressOf ExecuteTransaction1)
        Dim thread2 As New Thread(AddressOf ExecuteTransaction2)

        ' Start the threads
        thread1.Start()
        thread2.Start()

        ' Wait for both threads to finish
        thread1.Join()
        thread2.Join()

        ' Check for any exceptions
        If exceptions.Count > 0 Then
            Debug.WriteLine("Exceptions occurred:")
            For Each ex As Exception In exceptions
                Debug.WriteLine(ex.ToString())
                Throw ex
            Next
        Else
            Debug.WriteLine("No exceptions occurred.")
        End If

        Debug.WriteLine("Simulation complete.")
    End Sub

    Private Shared Sub ExecuteTransaction1()
        Using connection As New SqlConnection(connectionString)
            connection.Open()

            Dim command As SqlCommand = connection.CreateCommand()
            Dim transaction As SqlTransaction = Nothing

            Try
                transaction = connection.BeginTransaction()
                command.Transaction = transaction

                command.CommandText = "SELECT * FROM tblOrdersInProcess WITH (UPDLOCK)"
                command.ExecuteNonQuery()

                Debug.WriteLine("Thread 1 acquired tblOrdersInProcess lock.")

                ' Introduce a delay to increase the chance of deadlock
                Thread.Sleep(1000)

                command.CommandText = "SELECT * FROM tblOrderIntegrationMatch WITH (UPDLOCK)"
                command.ExecuteNonQuery()

                Debug.WriteLine("Thread 1 acquired tblOrderIntegrationMatch lock.")

                ' Commit the transaction
                transaction.Commit()
                Debug.WriteLine("Transaction 1 committed.")
            Catch ex As Exception
                Debug.WriteLine("Transaction 1 rolled back.")
                transaction?.Rollback()
                exceptions.Add(ex)
            End Try
        End Using
    End Sub

    Private Shared Sub ExecuteTransaction2()
        Using connection As New SqlConnection(connectionString)
            connection.Open()

            Dim command As SqlCommand = connection.CreateCommand()
            Dim transaction As SqlTransaction = Nothing

            Try
                transaction = connection.BeginTransaction()
                command.Transaction = transaction

                command.CommandText = "SELECT * FROM tblOrderIntegrationMatch WITH (UPDLOCK)"
                command.ExecuteNonQuery()

                Debug.WriteLine("Thread 2 acquired tblOrderIntegrationMatch lock.")

                ' Introduce a delay to increase the chance of deadlock
                Thread.Sleep(1000)

                command.CommandText = "SELECT * FROM tblOrdersInProcess WITH (UPDLOCK)"
                command.ExecuteNonQuery()

                Debug.WriteLine("Thread 2 acquired tblOrdersInProcess lock.")

                ' Commit the transaction
                transaction.Commit()
                Debug.WriteLine("Transaction 2 committed.")
            Catch ex As Exception
                Debug.WriteLine("Transaction 2 rolled back.")
                transaction?.Rollback()
                exceptions.Add(ex)
            End Try
        End Using
    End Sub
End Class
