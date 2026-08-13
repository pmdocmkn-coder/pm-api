const mysql = require('mysql2/promise');

async function main() {
  const connection = await mysql.createConnection({
    host: 'localhost',
    user: 'root',
    password: '@Password1!',
    database: 'mknpmlocal',
    port: 3306
  });

  console.log('Connected to DB');

  // Fix the Handover ContainsMainRadioUnit flag
  const [res1] = await connection.execute(
    `UPDATE RadioHandovers SET ContainsMainRadioUnit = 0 WHERE HandoverNumber = 'STR-202608-114'`
  );
  console.log('RadioHandovers update:', res1.affectedRows);

  // Fix the RadioRepairJobs status back to HandedToWarehouse
  const [res2] = await connection.execute(
    `UPDATE RadioRepairJobs SET Status = 'HandedToWarehouse', ClosedAt = NULL WHERE HelpdeskTicketNumber = 'MKN/0826/0389'`
  );
  console.log('RadioRepairJobs update:', res2.affectedRows);

  await connection.end();
}

main().catch(console.error);
