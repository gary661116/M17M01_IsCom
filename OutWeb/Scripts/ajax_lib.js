function handleAjaxVPNMsg(data) {
    var firstRemoveIndexStart = data.indexOf("<SCRIPT");
    var firstRemoveIndexEnd = data.indexOf("<\/SCRIPT>");
    var newData = data;
    while (firstRemoveIndexStart != -1) {
        firstRemoveIndexStart = newData.indexOf("<SCRIPT");
        firstRemoveIndexEnd = newData.indexOf("<\/SCRIPT>");
        //alert('firstRemoveIndexStart:'+firstRemoveIndexStart);
        if (firstRemoveIndexStart != -1) {
            if (firstRemoveIndexStart == 0) {
                newData = newData.substr(firstRemoveIndexEnd + 9, newData.length);
            } else {
                newDataHead = newData.substr(0, firstRemoveIndexStart);
                newDataTail = newData.substr(firstRemoveIndexEnd + 9, newData.length);
                //alert('newDataHead'+newDataHead);
                //alert('newDataTail'+newDataTail);
                newData = newDataHead + newDataTail;
            }
        }
        //alert(newData);
    }
    //alert("before:second:"+newData);
    var secondeRemoveIndex = newData.indexOf("<\/noscript>");
    //alert(secondeRemoveIndex);
    if (secondeRemoveIndex != -1) {
        newData = newData = newData.substr(0, secondeRemoveIndex);
    }
    //alert(newData);
    return newData;
}