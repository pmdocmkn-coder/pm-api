const sqlite3 = require('sqlite3').verbose();
const db = new sqlite3.Database('c:/Users/jupri.eka/CODE PM/Backend/pm-api/pm.db', (err) => {
  if (err) {
    console.error(err.message);
  }
  console.log('Connected to the pm.db database.');
});

db.serialize(() => {
  db.each(`SELECT Id, Username, FullName, RoleId FROM Users`, (err, row) => {
    if (err) {
      console.error(err.message);
    }
    console.log(row.Id + "\t" + row.Username + "\t" + row.FullName);
  });
});

db.close();
