USE [XCLCMS]
GO
/****** Object:  View [dbo].[v_SysDic_Roles]    Script Date: 04/04/2015 18:45:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_SysDic_Roles] AS
--角色信息
SELECT * FROM dbo.fun_SysDic_GetAllUnderListByCode('SysRoles')
GO
