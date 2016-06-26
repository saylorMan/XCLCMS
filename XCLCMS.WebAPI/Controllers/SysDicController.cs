﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using XCLCMS.Data.WebAPIEntity;
using XCLNetTools.Generic;

namespace XCLCMS.WebAPI.Controllers
{
    /// <summary>
    /// 字典库 管理
    /// </summary>
    public class SysDicController : BaseAPIController
    {
        private XCLCMS.Data.BLL.MerchantApp merchantAppBLL = new Data.BLL.MerchantApp();
        private XCLCMS.Data.BLL.SysDic sysDicBLL = new Data.BLL.SysDic();
        private XCLCMS.Data.BLL.Merchant merchantBLL = new XCLCMS.Data.BLL.Merchant();
        private XCLCMS.Data.BLL.View.v_SysDic vSysDicBLL = new Data.BLL.View.v_SysDic();

        /// <summary>
        /// 查询字典信息实体
        /// </summary>
        [HttpGet]
        [XCLCMS.Lib.Filters.FunctionFilter(Function = XCLCMS.Lib.Permission.Function.FunctionEnum.SysFun_Set_SysDicView)]
        public APIResponseEntity<XCLCMS.Data.Model.SysDic> Detail([FromUri] string json)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestEntity<long>>(System.Web.HttpUtility.UrlDecode(json));
            var response = new APIResponseEntity<XCLCMS.Data.Model.SysDic>();
            response.Body = this.sysDicBLL.GetModel(request.Body);
            response.IsSuccess = true;

            //限制商户
            if (base.IsOnlyCurrentMerchant && null != response.Body && response.Body.FK_MerchantID != base.CurrentUserModel.FK_MerchantID)
            {
                response.Body = null;
                response.IsSuccess = false;
            }

            return response;
        }

        /// <summary>
        /// 判断字典的唯一标识是否已经存在
        /// </summary>
        [HttpGet]
        public APIResponseEntity<bool> IsExistSysDicCode([FromUri] string json)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestEntity<XCLCMS.Data.WebAPIEntity.RequestEntity.SysDic.IsExistSysDicCodeEntity>>(System.Web.HttpUtility.UrlDecode(json));
            var response = new APIResponseEntity<bool>();
            response.IsSuccess = true;
            response.Message = "该字典标识可以使用！";

            XCLCMS.Data.Model.SysDic model = null;
            if (request.Body.SysDicID > 0)
            {
                model = this.sysDicBLL.GetModel(request.Body.SysDicID);
                if (null != model)
                {
                    if (string.Equals(request.Body.Code, model.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        return response;
                    }
                }
            }
            if (!string.IsNullOrEmpty(request.Body.Code))
            {
                bool isExist = new XCLCMS.Data.BLL.SysDic().IsExistCode(request.Body.Code);
                if (isExist)
                {
                    response.IsSuccess = false;
                    response.Message = "该字典标识已被占用！";
                    return response;
                }
            }
            return response;
        }

        /// <summary>
        /// 判断字典名，在同一级别中是否存在
        /// </summary>
        [HttpGet]
        public APIResponseEntity<bool> IsExistSysDicNameInSameLevel([FromUri] string json)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestEntity<XCLCMS.Data.WebAPIEntity.RequestEntity.SysDic.IsExistSysDicNameInSameLevelEntity>>(System.Web.HttpUtility.UrlDecode(json));
            var response = new APIResponseEntity<bool>();
            response.IsSuccess = true;
            response.Message = "该字典名可以使用！";

            XCLCMS.Data.Model.SysDic model = null;

            if (request.Body.SysDicID > 0)
            {
                model = this.sysDicBLL.GetModel(request.Body.SysDicID);
                if (null != model)
                {
                    if (string.Equals(request.Body.SysDicName, model.DicName, StringComparison.OrdinalIgnoreCase))
                    {
                        return response;
                    }
                }
            }

            List<XCLCMS.Data.Model.SysDic> lst = this.sysDicBLL.GetChildListByID(request.Body.ParentID);
            if (lst.IsNotNullOrEmpty())
            {
                if (lst.Exists(k => string.Equals(k.DicName, request.Body.SysDicName, StringComparison.OrdinalIgnoreCase)))
                {
                    response.IsSuccess = false;
                    response.Message = "该字典名在当前层级中已存在！";
                    return response;
                }
            }
            return response;
        }

        /// <summary>
        /// 根据code来获取字典的easyui tree格式
        /// </summary>
        [HttpGet]
        public APIResponseEntity<List<XCLNetTools.Entity.EasyUI.TreeItem>> GetEasyUITreeByCode([FromUri] string json)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestEntity<XCLCMS.Data.WebAPIEntity.RequestEntity.SysDic.GetEasyUITreeByCodeEntity>>(System.Web.HttpUtility.UrlDecode(json));
            var response = new APIResponseEntity<List<XCLNetTools.Entity.EasyUI.TreeItem>>();
            response.IsSuccess = true;

            List<XCLNetTools.Entity.EasyUI.TreeItem> tree = new List<XCLNetTools.Entity.EasyUI.TreeItem>();

            if (string.IsNullOrEmpty(request.Body.Code))
            {
                response.IsSuccess = true;
                response.Body = new List<XCLNetTools.Entity.EasyUI.TreeItem>();
                return response;
            }

            var rootModel = sysDicBLL.GetModelByCode(request.Body.Code);
            if (null == rootModel)
            {
                response.IsSuccess = true;
                response.Body = new List<XCLNetTools.Entity.EasyUI.TreeItem>();
                return response;
            }

            var allData = this.vSysDicBLL.GetAllUnderListByCode(request.Body.Code);
            var rootLayer = allData.Where(k => k.ParentID == rootModel.SysDicID).ToList();
            if (rootLayer.IsNotNullOrEmpty())
            {
                for (int idx = 0; idx < rootLayer.Count; idx++)
                {
                    var current = rootLayer[idx];

                    tree.Add(new XCLNetTools.Entity.EasyUI.TreeItem()
                    {
                        ID = current.SysDicID.ToString(),
                        Text = current.DicName
                    });

                    Action<XCLNetTools.Entity.EasyUI.TreeItem> getChildAction = null;
                    getChildAction = new Action<XCLNetTools.Entity.EasyUI.TreeItem>((parentModel) =>
                    {
                        var childs = allData.Where(k => k.ParentID == Convert.ToInt64(parentModel.ID)).ToList();
                        if (childs.IsNotNullOrEmpty())
                        {
                            parentModel.Children = new List<XCLNetTools.Entity.EasyUI.TreeItem>();
                            childs.ForEach(m =>
                            {
                                var treeItem = new XCLNetTools.Entity.EasyUI.TreeItem()
                                {
                                    ID = m.SysDicID.ToString(),
                                    State = m.IsLeaf == 1 ? "open" : "closed",
                                    Text = m.DicName
                                };
                                getChildAction(treeItem);
                                parentModel.Children.Add(treeItem);
                            });
                        }
                    });

                    getChildAction(tree.Find(k => k.ID == current.SysDicID.ToString()));
                }
            }

            response.IsSuccess = true;
            response.Body = tree;

            return response;
        }

        /// <summary>
        /// 查询所有字典列表
        /// </summary>
        [HttpGet]
        [XCLCMS.Lib.Filters.FunctionFilter(Function = XCLCMS.Lib.Permission.Function.FunctionEnum.SysFun_Set_SysDicView)]
        public APIResponseEntity<List<XCLCMS.Data.Model.View.v_SysDic>> GetList(string json)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestEntity<long>>(System.Web.HttpUtility.UrlDecode(json));
            var response = new APIResponseEntity<List<XCLCMS.Data.Model.View.v_SysDic>>();
            response.Body = this.vSysDicBLL.GetList(request.Body);
            response.IsSuccess = true;

            //限制商户
            if (base.IsOnlyCurrentMerchant && null != response.Body && response.Body.Count > 0)
            {
                response.Body = response.Body.Where(k => k.FK_MerchantID <= 0 || k.FK_MerchantID == base.CurrentUserModel.FK_MerchantID).ToList();
            }

            return response;
        }

        /// <summary>
        /// 根据条件获取字典的easy tree 列表
        /// </summary>
        public APIResponseEntity<List<XCLNetTools.Entity.EasyUI.TreeItem>> GetEasyUITreeByCondition([FromUri] string json)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestEntity<XCLCMS.Data.WebAPIEntity.RequestEntity.SysDic.GetEasyUITreeByConditionEntity>>(System.Web.HttpUtility.UrlDecode(json));
            var response = new APIResponseEntity<List<XCLNetTools.Entity.EasyUI.TreeItem>>();
            response.IsSuccess = true;

            List<XCLCMS.Data.Model.View.v_SysDic> allData = null;
            List<XCLNetTools.Entity.EasyUI.TreeItem> tree = new List<XCLNetTools.Entity.EasyUI.TreeItem>();

            var merchantModel = this.merchantBLL.GetModel(request.Body.MerchantID);
            if (null == merchantModel)
            {
                response.IsSuccess = false;
                response.Message = "您指定的商户号无效！";
                return response;
            }

            allData = this.vSysDicBLL.GetModelList("");

            if (request.Body.MerchantID > 0 && null != allData && allData.Count > 0)
            {
                allData = allData.Where(k => k.FK_MerchantID <= 0 || k.FK_MerchantID == request.Body.MerchantID).ToList();
            }

            if (allData.IsNotNullOrEmpty())
            {
                var root = allData.Where(k => k.ParentID == 0).FirstOrDefault();//根节点
                if (null != root)
                {
                    tree.Add(new XCLNetTools.Entity.EasyUI.TreeItem()
                    {
                        ID = root.SysDicID.ToString(),
                        State = root.IsLeaf == 1 ? "open" : "closed",
                        Text = root.DicName
                    });

                    Action<XCLNetTools.Entity.EasyUI.TreeItem> getChildAction = null;
                    getChildAction = new Action<XCLNetTools.Entity.EasyUI.TreeItem>((parentModel) =>
                    {
                        var childs = allData.Where(k => k.ParentID == Convert.ToInt64(parentModel.ID)).ToList();
                        if (childs.IsNotNullOrEmpty())
                        {
                            childs = childs.OrderBy(k => k.Sort).ToList();
                            parentModel.Children = new List<XCLNetTools.Entity.EasyUI.TreeItem>();
                            childs.ForEach(m =>
                            {
                                var treeItem = new XCLNetTools.Entity.EasyUI.TreeItem()
                                {
                                    ID = m.SysDicID.ToString(),
                                    State = m.IsLeaf == 1 ? "open" : "closed",
                                    Text = m.DicName
                                };
                                getChildAction(treeItem);
                                parentModel.Children.Add(treeItem);
                            });
                        }
                    });

                    //从根节点开始
                    getChildAction(tree[0]);
                }
            }
            response.Body = tree;
            return response;
        }

        /// <summary>
        /// 获取XCLCMS管理后台系统的菜单
        /// </summary>
        [HttpGet]
        public APIResponseEntity<List<XCLCMS.Data.Model.View.v_SysDic>> GetSystemMenuModelList(string json)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestEntity<object>>(System.Web.HttpUtility.UrlDecode(json));
            var response = new APIResponseEntity<List<XCLCMS.Data.Model.View.v_SysDic>>();
            response.Body = this.vSysDicBLL.GetSystemMenuModelList();
            response.IsSuccess = true;
            return response;
        }

        /// <summary>
        /// 根据SysDicID查询其子项
        /// </summary>
        [HttpGet]
        public APIResponseEntity<List<XCLCMS.Data.Model.SysDic>> GetChildListByID(string json)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestEntity<long>>(System.Web.HttpUtility.UrlDecode(json));
            var response = new APIResponseEntity<List<XCLCMS.Data.Model.SysDic>>();
            response.Body = this.sysDicBLL.GetChildListByID(request.Body);
            response.IsSuccess = true;
            return response;
        }

        /// <summary>
        /// 获取当前sysDicID所属的层级list
        /// 如:根目录/子目录/文件
        /// </summary>
        [HttpGet]
        public APIResponseEntity<List<XCLCMS.Data.Model.Custom.SysDicSimple>> GetLayerListBySysDicID(string json)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestEntity<XCLCMS.Data.WebAPIEntity.RequestEntity.SysDic.GetLayerListBySysDicIDEntity>>(System.Web.HttpUtility.UrlDecode(json));
            var response = new APIResponseEntity<List<XCLCMS.Data.Model.Custom.SysDicSimple>>();
            response.Body = this.sysDicBLL.GetLayerListBySysDicID(request.Body.SysDicID);
            response.IsSuccess = true;
            return response;
        }

        /// <summary>
        /// 获取证件类型
        /// </summary>
        [HttpGet]
        public APIResponseEntity<Dictionary<string, long>> GetPassTypeDic(string json)
        {
            var response = new APIResponseEntity<Dictionary<string, long>>();
            response.Body = this.sysDicBLL.GetPassTypeDic();
            response.IsSuccess = true;
            return response;
        }

        /// <summary>
        /// 递归获取指定SysDicID下的所有列表（不包含该SysDicID的记录）
        /// </summary>
        [HttpGet]
        public APIResponseEntity<List<XCLCMS.Data.Model.View.v_SysDic>> GetAllUnderListByID(string json)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<APIRequestEntity<long>>(System.Web.HttpUtility.UrlDecode(json));
            var response = new APIResponseEntity<List<XCLCMS.Data.Model.View.v_SysDic>>();
            response.Body = this.vSysDicBLL.GetAllUnderListByID(request.Body);
            response.IsSuccess = true;
            return response;
        }

        /// <summary>
        /// 添加字典
        /// </summary>
        [HttpPost]
        [XCLCMS.Lib.Filters.FunctionFilter(Function = XCLCMS.Lib.Permission.Function.FunctionEnum.SysFun_Set_SysDicAdd)]
        public APIResponseEntity<bool> Add(JObject obj)
        {
            var request = obj.ToObject<APIRequestEntity<XCLCMS.Data.Model.SysDic>>();
            var response = new APIResponseEntity<bool>();

            #region 数据校验

            request.Body.DicName = (request.Body.DicName ?? "").Trim();
            request.Body.Code = (request.Body.Code ?? "").Trim();

            //字典名必填
            if (string.IsNullOrEmpty(request.Body.DicName))
            {
                response.IsSuccess = false;
                response.Message = "请提供字典名！";
                return response;
            }

            //若有code，则判断是否唯一
            if (!string.IsNullOrEmpty(request.Body.Code))
            {
                if (this.sysDicBLL.IsExistCode(request.Body.Code))
                {
                    response.IsSuccess = false;
                    response.Message = string.Format("字典唯一标识【{0}】已存在！", request.Body.Code);
                    return response;
                }
            }

            //父字典是否存在
            var parentNodeModel = this.sysDicBLL.GetModel(request.Body.ParentID);
            if (null == parentNodeModel)
            {
                response.IsSuccess = false;
                response.Message = "父字典不存在！";
                return response;
            }

            //限制商户
            if (base.IsOnlyCurrentMerchant && request.Body.FK_MerchantID != base.CurrentUserModel.FK_MerchantID)
            {
                response.IsSuccess = false;
                response.Message = "只能在自己所属的商户下面添加字典信息！";
                return response;
            }

            //当前用户只能加在自己的商户号下面
            if (!this.vSysDicBLL.IsRoot(parentNodeModel.SysDicID))
            {
                if (parentNodeModel.FK_MerchantID != request.Body.FK_MerchantID)
                {
                    response.IsSuccess = false;
                    response.Message = "您添加的字典必须与父字典在同一个商户中！";
                    return response;
                }
            }

            //应用号与商户一致
            if (!this.merchantAppBLL.IsTheSameMerchantInfoID(request.Body.FK_MerchantID, request.Body.FK_MerchantAppID))
            {
                response.IsSuccess = false;
                response.Message = "商户号与应用号不匹配，请核对后再试！";
                return response;
            }

            #endregion 数据校验

            if (this.sysDicBLL.Add(request.Body))
            {
                response.IsSuccess = true;
                response.Message = "添加成功！";
            }
            else
            {
                response.IsSuccess = false;
                response.Message = "添加失败！";
            }

            return response;
        }

        /// <summary>
        /// 修改字典
        /// </summary>
        [HttpPost]
        [XCLCMS.Lib.Filters.FunctionFilter(Function = XCLCMS.Lib.Permission.Function.FunctionEnum.SysFun_Set_SysDicEdit)]
        public APIResponseEntity<bool> Update(JObject obj)
        {
            var request = obj.ToObject<APIRequestEntity<XCLCMS.Data.Model.SysDic>>();
            var response = new APIResponseEntity<bool>();

            #region 数据校验

            var model = this.sysDicBLL.GetModel(request.Body.SysDicID);
            if (null == model)
            {
                response.IsSuccess = false;
                response.Message = "请指定有效的字典信息！";
                return response;
            }

            request.Body.DicName = (request.Body.DicName ?? "").Trim();
            request.Body.Code = (request.Body.Code ?? "").Trim();

            //字典名必填
            if (string.IsNullOrEmpty(request.Body.DicName))
            {
                response.IsSuccess = false;
                response.Message = "请提供字典名！";
                return response;
            }

            //若有code，则判断是否唯一
            if (!string.IsNullOrEmpty(request.Body.Code))
            {
                if (!string.Equals(model.Code, request.Body.Code) && this.sysDicBLL.IsExistCode(request.Body.Code))
                {
                    response.IsSuccess = false;
                    response.Message = string.Format("字典唯一标识【{0}】已存在！", request.Body.Code);
                    return response;
                }
            }

            //限制商户
            if (base.IsOnlyCurrentMerchant && request.Body.FK_MerchantID != base.CurrentUserModel.FK_MerchantID)
            {
                response.IsSuccess = false;
                response.Message = "只能在自己所属的商户下面修改字典信息！";
                return response;
            }

            //应用号与商户一致
            if (!this.merchantAppBLL.IsTheSameMerchantInfoID(request.Body.FK_MerchantID, request.Body.FK_MerchantAppID))
            {
                response.IsSuccess = false;
                response.Message = "商户号与应用号不匹配，请核对后再试！";
                return response;
            }

            #endregion 数据校验

            model.Code = request.Body.Code;
            model.DicName = request.Body.DicName;
            model.DicValue = request.Body.DicValue;
            model.FK_FunctionID = request.Body.FK_FunctionID;
            model.FK_MerchantAppID = request.Body.FK_MerchantAppID;
            model.ParentID = request.Body.ParentID;
            model.RecordState = request.Body.RecordState;
            model.Remark = request.Body.Remark;
            model.Sort = request.Body.Sort;
            model.UpdaterID = base.CurrentUserModel.UserInfoID;
            model.UpdaterName = base.CurrentUserModel.UserName;
            model.UpdateTime = DateTime.Now;

            if (this.sysDicBLL.Update(model))
            {
                response.IsSuccess = true;
                response.Message = "修改成功！";
            }
            else
            {
                response.IsSuccess = false;
                response.Message = "修改失败！";
            }

            return response;
        }

        /// <summary>
        /// 删除字典
        /// </summary>
        [HttpPost]
        [XCLCMS.Lib.Filters.FunctionFilter(Function = XCLCMS.Lib.Permission.Function.FunctionEnum.SysFun_Set_SysDicDel)]
        public APIResponseEntity<bool> Delete(JObject obj)
        {
            var request = obj.ToObject<APIRequestEntity<List<long>>>();
            var response = new APIResponseEntity<bool>();

            if (null == request.Body || request.Body.Count == 0)
            {
                response.IsSuccess = false;
                response.Message = "请指定要删除的字典ID！";
                return response;
            }

            request.Body = request.Body.Distinct().ToList();

            //只能删除自己商户的节点
            if (base.IsOnlyCurrentMerchant)
            {
                if (request.Body.Exists(id =>
                {
                    var sysDicModel = sysDicBLL.GetModel(id);
                    return null != sysDicModel && sysDicModel.FK_MerchantID != base.CurrentUserModel.FK_MerchantID;
                }))
                {
                    response.IsSuccess = false;
                    response.Message = "只能删除属于自己的商户节点！";
                    return response;
                }
            }

            int successCount = 0;

            request.Body.ForEach(id =>
            {
                var sysDicModel = sysDicBLL.GetModel(id);
                if (null != sysDicModel)
                {
                    sysDicModel.UpdaterID = base.CurrentUserModel.UserInfoID;
                    sysDicModel.UpdaterName = base.CurrentUserModel.UserName;
                    sysDicModel.UpdateTime = DateTime.Now;
                    sysDicModel.RecordState = XCLCMS.Data.CommonHelper.EnumType.RecordStateEnum.D.ToString();
                    if (sysDicBLL.Update(sysDicModel))
                    {
                        successCount++;
                    }
                }
            });

            response.IsSuccess = true;
            response.Message = string.Format("已成功删除【{0}】条记录！", successCount);

            return response;
        }

        /// <summary>
        /// 删除指定字典的所有节点
        /// </summary>
        [HttpPost]
        [XCLCMS.Lib.Filters.FunctionFilter(Function = XCLCMS.Lib.Permission.Function.FunctionEnum.SysFun_Set_SysDicDel)]
        public APIResponseEntity<bool> DelChild(JObject obj)
        {
            var request = obj.ToObject<APIRequestEntity<long>>();
            var response = new APIResponseEntity<bool>();

            if (request.Body <= 0)
            {
                response.IsSuccess = false;
                response.Message = "请指定要删除所有子节点的字典ID！";
                return response;
            }

            if (base.IsOnlyCurrentMerchant)
            {
                var sysDicModel = sysDicBLL.GetModel(request.Body);
                if (null != sysDicModel && sysDicModel.FK_MerchantID != base.CurrentUserModel.FK_MerchantID)
                {
                    response.IsSuccess = false;
                    response.Message = "只能删除属于自己的商户节点！";
                    return response;
                }
            }

            response.IsSuccess = this.sysDicBLL.DelChild(new Data.Model.SysDic()
            {
                SysDicID = request.Body,
                UpdaterID = base.CurrentUserModel.UserInfoID,
                UpdaterName = base.CurrentUserModel.UserName,
                UpdateTime = DateTime.Now
            });

            if (response.IsSuccess)
            {
                response.Message = "成功删除所有子节点！";
            }
            else
            {
                response.Message = "删除所有子节点失败！";
            }

            return response;
        }
    }
}