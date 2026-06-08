package main

import (
	"log"
	"net/http"
	"time"
)

func handleReport(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	gameKey := r.URL.Query().Get("game_key")
	apiKey := r.URL.Query().Get("api_key")
	onlineCount := r.URL.Query().Get("online_count")

	log.Printf("[%s] game_key=%s api_key=%s online_count=%s",
		time.Now().Format(time.RFC3339), gameKey, apiKey, onlineCount)

	w.WriteHeader(http.StatusOK)
}

func main() {
	http.HandleFunc("/report", handleReport)
	log.Println("active-reporter-test listening on :8080")
	if err := http.ListenAndServe(":8080", nil); err != nil {
		log.Fatalf("server error: %v", err)
	}
}
