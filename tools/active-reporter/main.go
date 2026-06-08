package main

import (
	"database/sql"
	"fmt"
	"log"
	"net/http"
	"net/url"
	"os"
	"strconv"
	"strings"

	_ "github.com/lib/pq"
)

const (
	gameKey        = "nisekue"
	defaultBaseURL = "http://localhost:8080/report"
)

func parseDotnetConnectionString(cs string) (string, error) {
	params := make(map[string]string)
	for _, part := range strings.Split(cs, ";") {
		part = strings.TrimSpace(part)
		if part == "" {
			continue
		}
		idx := strings.Index(part, "=")
		if idx < 0 {
			continue
		}
		key := strings.ToLower(strings.TrimSpace(part[:idx]))
		val := strings.TrimSpace(part[idx+1:])
		params[key] = val
	}

	host := params["host"]
	port := params["port"]
	dbname := params["database"]
	user := params["username"]
	password := params["password"]

	if host == "" || dbname == "" || user == "" {
		return "", fmt.Errorf("connection string missing required fields (host, database, username)")
	}
	if port == "" {
		port = "5432"
	}

	sslMode := "require"
	if host == "127.0.0.1" || host == "localhost" {
		sslMode = "disable"
	}

	return fmt.Sprintf("host=%s port=%s dbname=%s user=%s password=%s sslmode=%s",
		host, port, dbname, user, password, sslMode), nil
}

func countActivePlayers(db *sql.DB) (int, error) {
	var count int
	err := db.QueryRow(`
		SELECT COUNT(*)
		FROM internal.players
		WHERE last_active_at >= NOW() - INTERVAL '5 minutes'
	`).Scan(&count)
	return count, err
}

func sendReport(baseURL, apiKey string, count int) error {
	u, err := url.Parse(baseURL)
	if err != nil {
		return fmt.Errorf("parse base url: %w", err)
	}

	q := u.Query()
	q.Set("game_key", gameKey)
	q.Set("api_key", apiKey)
	q.Set("online_count", strconv.Itoa(count))
	u.RawQuery = q.Encode()

	resp, err := http.Get(u.String())
	if err != nil {
		return fmt.Errorf("get: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode < 200 || resp.StatusCode >= 300 {
		return fmt.Errorf("unexpected status: %s", resp.Status)
	}
	return nil
}

func main() {
	connStr := os.Getenv("SUPABASE_DB_CONNECTION_STRING")
	if connStr == "" {
		log.Fatal("SUPABASE_DB_CONNECTION_STRING is not set")
	}

	baseURL := os.Getenv("REPORT_ENDPOINT_URL")
	if baseURL == "" {
		baseURL = defaultBaseURL
	}

	apiKey := os.Getenv("PORTAL_API_KEY")

	dsn, err := parseDotnetConnectionString(connStr)
	if err != nil {
		log.Fatalf("failed to parse connection string: %v", err)
	}

	db, err := sql.Open("postgres", dsn)
	if err != nil {
		log.Fatalf("failed to open database: %v", err)
	}
	defer db.Close()

	count, err := countActivePlayers(db)
	if err != nil {
		log.Fatalf("failed to count active players: %v", err)
	}

	if err := sendReport(baseURL, apiKey, count); err != nil {
		log.Fatalf("failed to send report: %v", err)
	}

	log.Printf("reported %d active players to %s", count, baseURL)
}
