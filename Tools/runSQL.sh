for file in $1/*.sql; do
	sudo -u postgres psql postgres -f "$file"
done