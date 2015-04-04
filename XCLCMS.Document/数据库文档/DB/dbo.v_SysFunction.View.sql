USE [XCLCMS]
GO
/****** Object:  View [dbo].[v_SysFunction]    Script Date: 04/04/2015 18:45:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_SysFunction] AS

WITH Info1 AS (
	SELECT * FROM dbo.fun_SysDic_GetAllUnderListByCode('SysFunModules')
)
SELECT
a.*,
b.DicName AS C_TypeName
FROM dbo.SysFunction AS a
INNER JOIN Info1 AS b ON a.FK_TypeID=b.SysDicID AND a.RecordState='N'
GO
