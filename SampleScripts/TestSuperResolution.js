// 测试超分功能脚本
// 使用方法: 打开一张图片后运行此脚本

(function() {
    var nv = host;
    var book = nv.Book;
    
    if (!book.IsLoaded) {
        nv.ShowToast("❌ 请先打开一本书/图片");
        return;
    }
    
    var currentPage = book.CurrentPage;
    if (!currentPage) {
        nv.ShowToast("❌ 当前没有页面");
        return;
    }
    
    // 显示当前图片信息
    var info = "📊 当前图片信息:\n";
    info += "路径: " + currentPage.EntryFullName + "\n";
    info += "尺寸: " + currentPage.Width + " x " + currentPage.Height + "\n";
    info += "文件大小: " + (currentPage.Size / 1024).toFixed(2) + " KB\n";
    
    nv.ShowToast(info);
    
    console.log("=== 超分测试 ===");
    console.log("图片路径:", currentPage.EntryFullName);
    console.log("原始尺寸:", currentPage.Width, "x", currentPage.Height);
    console.log("文件大小:", (currentPage.Size / 1024).toFixed(2), "KB");
    
    // 提示用户查看日志
    nv.ShowMessage("请查看日志文件确认超分是否执行: %LocalAppData%\\NeeView\\Logs\\SuperResolution_DEV_*.log");
    
})();
