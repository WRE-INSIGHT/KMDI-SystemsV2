﻿Imports System.ComponentModel
Imports System.Data.SqlClient
Imports System.Threading.Thread
Imports ComponentFactory.Krypton.Toolkit
Public Class Engr_STWDT_Maintenance
    Public STWDT_BGW As BackgroundWorker = New BackgroundWorker
    Dim ST_ID, WDT_ID As Integer
    Dim STWDT_TODO, SystemType_Str, WindowType_Str As String
    Dim T_FACTOR As TimeSpan
    Dim ReportBGW_bool As Boolean
    Dim DGVrow_list As New List(Of Object)
    Sub Start_STWDTBGW()
        If STWDT_BGW.IsBusy <> True Then
            LoadingPB.Visible = True
            Frm_PNL.Enabled = False
            STWDT_BGW.RunWorkerAsync()
        Else
            KMDIPrompts(Me, "UserWarning", Nothing, Nothing, Nothing, True, True, "Please Wait!")
        End If
    End Sub
    Private Sub Engr_STWDT_Maintenance_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            STWDT_BGW.WorkerSupportsCancellation = True
            STWDT_BGW.WorkerReportsProgress = True
            AddHandler STWDT_BGW.DoWork, AddressOf STWDT_BGW_DoWork
            AddHandler STWDT_BGW.ProgressChanged, AddressOf STWDT_BGW_ProgressChanged
            AddHandler STWDT_BGW.RunWorkerCompleted, AddressOf STWDT_BGW_RunWorkerCompleted
            STWDT_TODO = "Onload"
            Start_STWDTBGW()
        Catch ex As Exception
            KMDIPrompts(Me, "DotNetError", ex.Message, ex.StackTrace)
        End Try
    End Sub
    Private Sub STWDT_BGW_DoWork(sender As Object, e As DoWorkEventArgs)
        Try
            If STWDT_TODO = "SystemType" Or STWDT_TODO = "Onload" Then
                ReportBGW_bool = True
                ENGR_QUERY_INSTANCE = "Loading_using_SearchString"
                Engr_Query_Select_STP("", "ENGR_stp_STWDT_SystemType")
            ElseIf STWDT_TODO = "WindowType" Then
                ReportBGW_bool = True
                ENGR_QUERY_INSTANCE = "Loading_using_SearchString"
                Engr_Query_Select_STP("", "ENGR_stp_STWDT_WindowType")
            ElseIf STWDT_TODO = "SystemType_Insert" Then
                Engr_SystemType_INSERT("ENGR_stp_STWDT_SystemType_Insert", SystemType_Str)
            ElseIf STWDT_TODO = "WindowType_Insert" Then
                Engr_WindowType_INSERT("ENGR_stp_STWDT_WindowType_Insert", WindowType_Str)
            ElseIf STWDT_TODO = "Fetch_TFactor" Then
                Engr_Query_Select_T_FACTOR(ST_ID, WDT_ID, "ENGR_stp_STWDT_TFactor_Fetch")
            ElseIf STWDT_TODO = "Transact_TFactor" Then
                Engr_TFactor_Transact("ENGR_stp_STWDT_TFactor_Transact", ST_ID, WDT_ID, T_FACTOR)
            End If

            Select Case ReportBGW_bool
                Case True
                    For i = 0 To sqlDataSet.Tables("QUERY_DETAILS").Rows.Count - 1
                        Sleep(100)
                        STWDT_BGW.ReportProgress(i)
                    Next
            End Select
        Catch SQLex As SqlException
            'DisplaySqlErrors(ex) 'Galing to sa KMDI_V1 -->Marketing_Analysis.vb (line 28)
            'Dito ako naglagay ng SqlException dahil hindi makaCancel ang BGW sa ibang Class
            sql_err_bool = True
            STWDT_BGW.CancelAsync()
            KMDIPrompts(Me, "SqlError", SQLex.Message, SQLex.StackTrace, SQLex.Number, True)
            Try
                transaction.Rollback()
                sql_Transaction_result = "Rollback"
            Catch ex2 As Exception
                KMDIPrompts(Me, "SqlError", ex2.Message, ex2.StackTrace)
            End Try
        Catch ex As Exception
            KMDIPrompts(Me, "DotNetError", ex.Message, ex.StackTrace, Nothing, True)
        End Try

        If STWDT_BGW.CancellationPending Then
            e.Cancel = True
        End If
    End Sub
    Private Sub STWDT_BGW_ProgressChanged(sender As Object, e As ProgressChangedEventArgs)
        Try
            Dim RdBtn As New MetroFramework.Controls.MetroRadioButton
            If STWDT_TODO = "SystemType" Or STWDT_TODO = "Onload" Then
                RdBtn_Properties("Dynamic", RdBtn, "SYSTEM_TYPE", "ST_ID", e.ProgressPercentage)
                STRdBtn_FLP.Controls.Add(RdBtn)
                AddHandler RdBtn.Click, AddressOf SysRbtn_Clicked
            ElseIf STWDT_TODO = "WindowType" Then
                RdBtn_Properties("Dynamic", RdBtn, "WINDOW_TYPE", "WDT_ID", e.ProgressPercentage)
                WDTRdBtn_FLP.Controls.Add(RdBtn)
                AddHandler RdBtn.Click, AddressOf WDTRbtn_Clicked
            End If
        Catch ex As Exception
            KMDIPrompts(Me, "DotNetError", ex.Message, ex.StackTrace, Nothing, True)
            LoadingPB.Visible = False
        End Try
    End Sub
    Private Sub STWDT_BGW_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs)
        Try
            If e.Error IsNot Nothing Or e.Cancelled = True Then
                ' if BackgroundWorker terminated due to error
                LoadingPB.Visible = False
            Else
                '' otherwise it completed normally
                If sql_Transaction_result = "Committed" Then
                    Select Case STWDT_TODO
                        Case "Onload"
                            STWDT_TODO = "WindowType"
                            Start_STWDTBGW()
                        Case "SystemType"
                            ReportBGW_bool = False
                        Case "WindowType"
                            ReportBGW_bool = False
                        Case "SystemType_Insert"
                            Dim RdBtn As New MetroFramework.Controls.MetroRadioButton
                            RdBtn_Properties("Static", RdBtn, SystemType_Str, InsertedST_ID)
                            STRdBtn_FLP.Controls.Add(RdBtn)
                            AddHandler RdBtn.Click, AddressOf SysRbtn_Clicked

                            SystemType_Tbox.Clear()
                            SystemType_Str = Nothing
                            InsertedST_ID = Nothing
                        Case "WindowType_Insert"
                            Dim RdBtn As New MetroFramework.Controls.MetroRadioButton
                            RdBtn_Properties("Static", RdBtn, WindowType_Str, InsertedWDT_ID)
                            WDTRdBtn_FLP.Controls.Add(RdBtn)
                            AddHandler RdBtn.Click, AddressOf SysRbtn_Clicked

                            WindowType_Tbox.Clear()
                            WindowType_Str = Nothing
                            InsertedWDT_ID = Nothing
                        Case "Fetch_TFactor"
                            If sqlDataSet.Tables("QUERY_DETAILS").Rows.Count <> 0 Then
                                T_FACTOR = TimeSpan.Parse(sqlDataSet.Tables("QUERY_DETAILS").Rows(0).Item("T_FACTOR").ToString)
                                TFactor_Tbox.Text = T_FACTOR.ToString("c")
                            ElseIf sqlDataSet.Tables("QUERY_DETAILS").Rows.Count = 0 Then
                                TFactor_Tbox.Clear()
                            End If
                        Case "Transact_TFactor"
                            KMDIPrompts(Me, "Success", Nothing, Nothing, Nothing, True)
                    End Select
                End If
            End If
        Catch ex As Exception
            KMDIPrompts(Me, "DotNetError", ex.Message, ex.StackTrace, Nothing, True)
        End Try
        RESET()
        LoadingPB.Visible = False
        Frm_PNL.Enabled = True
    End Sub
    Private Sub SysRbtn_Clicked(sender As Object, e As EventArgs)
        Try
            ST_ID = sender.Tag
            If WDT_ID <> Nothing Then
                STWDT_TODO = "Fetch_TFactor"
                Start_STWDTBGW()
            End If
        Catch ex As Exception
            KMDIPrompts(Me, "DotNetError", ex.Message, ex.StackTrace, Nothing, True)
        End Try
    End Sub
    Private Sub WDTRbtn_Clicked(sender As Object, e As EventArgs)
        Try
            WDT_ID = sender.Tag
            If ST_ID <> Nothing Then
                STWDT_TODO = "Fetch_TFactor"
                Start_STWDTBGW()
            End If
        Catch ex As Exception
            KMDIPrompts(Me, "DotNetError", ex.Message, ex.StackTrace, Nothing, True)
        End Try
    End Sub
    Private Sub SystemType_Tbox_ButtonClick(sender As Object, e As EventArgs) Handles SystemType_Tbox.ButtonClick
        Try
            SystemType_Str = Trim(SystemType_Tbox.Text)
            If SystemType_Str <> Nothing Or SystemType_Str <> "" Then
                STWDT_TODO = "SystemType_Insert"
                Start_STWDTBGW()
            Else
                KMDIPrompts(Me, "UserWarning", "SystemType_Tbox is Empty", Environment.StackTrace, Nothing, True, True, "Field cannot be empty")
            End If
        Catch ex As Exception
            KMDIPrompts(Me, "DotNetError", ex.Message, ex.StackTrace, Nothing, True)
        End Try
    End Sub

    Private Sub Engr_STWDT_Maintenance_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        Try
            If (e.Control And e.KeyCode = Keys.S) Then
                If TFactor_Tbox.MaskCompleted <> False Then
                    T_FACTOR = TimeSpan.Parse(TFactor_Tbox.Text)
                    If ST_ID <> Nothing Then
                        If WDT_ID <> Nothing Then
                            STWDT_TODO = "Transact_TFactor"
                            Start_STWDTBGW()
                        Else
                            KMDIPrompts(Me, "UserWarning", "WDT_ID is Empty", Environment.StackTrace, Nothing, True, True, "Please select Window Type")
                        End If
                    Else
                        KMDIPrompts(Me, "UserWarning", "ST_ID is Empty", Environment.StackTrace, Nothing, True, True, "Please select System Type")
                    End If
                Else
                    KMDIPrompts(Me, "UserWarning", "TFactor_Tbox is Empty", Environment.StackTrace, Nothing, True, True, "Complete the field")
                End If
            End If
        Catch ex As Exception
            KMDIPrompts(Me, "DotNetError", ex.Message, ex.StackTrace, Nothing, True)
        End Try
    End Sub

    Private Sub WindowType_Tbox_ButtonClick(sender As Object, e As EventArgs) Handles WindowType_Tbox.ButtonClick
        Try
            WindowType_Str = Trim(WindowType_Tbox.Text)
            If WindowType_Str <> Nothing Or WindowType_Str <> "" Then
                STWDT_TODO = "WindowType_Insert"
                Start_STWDTBGW()
            Else
                KMDIPrompts(Me, "UserWarning", "WindowType_Str is Empty", Environment.StackTrace, Nothing, True, True, "Field cannot be empty")
            End If
        Catch ex As Exception
            KMDIPrompts(Me, "DotNetError", ex.Message, ex.StackTrace, Nothing, True)
        End Try
    End Sub
    Private Sub SystemType_Tbox_KeyDown(sender As Object, e As KeyEventArgs) Handles SystemType_Tbox.KeyDown
        If e.KeyCode = Keys.Enter Then
            SystemType_Tbox.CustomButton.PerformClick()
        End If
    End Sub
    Private Sub WindowType_Tbox_KeyDown(sender As Object, e As KeyEventArgs) Handles WindowType_Tbox.KeyDown
        If e.KeyCode = Keys.Enter Then
            WindowType_Tbox.CustomButton.PerformClick()
        End If
    End Sub
End Class