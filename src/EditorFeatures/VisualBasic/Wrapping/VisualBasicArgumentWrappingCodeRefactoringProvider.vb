﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Editor.Wrapping
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicArgumentWrappingCodeRefactoringProvider
        Inherits AbstractVisualBasicWrappingCodeRefactoringProvider(Of ArgumentListSyntax, ArgumentSyntax)

        Protected Overrides ReadOnly Property ListName As String = FeaturesResources.argument_list
        Protected Overrides ReadOnly Property ItemNamePlural As String = FeaturesResources.arguments
        Protected Overrides ReadOnly Property ItemNameSingular As String = FeaturesResources.argument

        Protected Overrides Function GetListItems(listSyntax As ArgumentListSyntax) As SeparatedSyntaxList(Of ArgumentSyntax)
            Return listSyntax.Arguments
        End Function

        Protected Overrides Function GetApplicableList(node As SyntaxNode) As ArgumentListSyntax
            Return If(TryCast(node, InvocationExpressionSyntax)?.ArgumentList,
                      TryCast(node, ObjectCreationExpressionSyntax)?.ArgumentList)
        End Function

        Protected Overrides Function PositionIsApplicable(
                root As SyntaxNode, position As Integer,
                declaration As SyntaxNode, listSyntax As ArgumentListSyntax) As Boolean

            Dim startToken = listSyntax.GetFirstToken()

            ' If we have something Like  Foo(...)  Or  this.Foo(...)  allow anywhere in the Foo(...)
            If TypeOf declaration Is InvocationExpressionSyntax Then
                Dim expr = DirectCast(declaration, InvocationExpressionSyntax).Expression
                Dim name =
                    If(TryCast(expr, NameSyntax),
                       TryCast(expr, MemberAccessExpressionSyntax)?.Name)

                startToken = If(name Is Nothing, listSyntax.GetFirstToken(), name.GetFirstToken())
            ElseIf TypeOf declaration Is ObjectCreationExpressionSyntax Then
                ' allow anywhere in `New Foo(...)`
                startToken = declaration.GetFirstToken()
            End If

            Dim endToken = listSyntax.GetLastToken()
            Dim span = TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End)
            If Not span.IntersectsWith(position) Then
                Return False
            End If

            ' allow anywhere in the arg list, as long we don't end up walking through something
            ' complex Like a lambda/anonymous function.
            Dim token = root.FindToken(position)
            If token.Parent.Ancestors().Contains(listSyntax) Then
                Dim current = token.Parent
                While current IsNot listSyntax
                    If VisualBasicSyntaxFactsService.Instance.IsAnonymousFunction(current) Then
                        Return False
                    End If

                    current = current.Parent
                End While
            End If

            Return True
        End Function
    End Class
End Namespace
