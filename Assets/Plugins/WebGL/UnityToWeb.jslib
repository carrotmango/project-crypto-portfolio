mergeInto(LibraryManager.library, {
  SendMessageToWeb: function (msgPtr) {
    const message = UTF8ToString(msgPtr);
    console.log("[Unity → Web] 전송:", message);

    const allowedOrigins = [
      "https://carrotmango.xyz",
      "https://carrotmango.netlify.app",
      "http://localhost:5173",
    ];

    const referrer = document.referrer || "";
    const matchedOrigin = allowedOrigins.find(origin =>
      referrer.startsWith(origin)
    );

    if (window.parent && matchedOrigin) {
      window.parent.postMessage(
        {
          type: "UNITY_MESSAGE",
          payload: message,
        },
        matchedOrigin
      );
      console.log("[Unity → Web] postMessage 실행 완료:", matchedOrigin);
    } else if (window.parent) {
      console.warn("[UnityToWeb] 허용되지 않은 origin 또는 referrer:", referrer);
    } else {
      console.warn("[UnityToWeb] 부모창 없음 — iframe 외부 실행 중일 수 있음");
    }
  },
});
